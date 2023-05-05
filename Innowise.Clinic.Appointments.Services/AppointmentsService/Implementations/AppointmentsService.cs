using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;
using Innowise.Clinic.Appointments.Persistence.EntityFilters.Doctors;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
using Innowise.Clinic.Appointments.Services.Mappings;
using Innowise.Clinic.Appointments.Services.NotificationsService;
using Innowise.Clinic.Appointments.Services.TimeSlotsService.Interfaces;
using Innowise.Clinic.Shared.Constants;
using Innowise.Clinic.Shared.Exceptions;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Requests;
using Innowise.Clinic.Shared.Services.FiltrationService;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.PredicateBuilder;
using MassTransit;
using IdFilter = Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments.IdFilter;

namespace Innowise.Clinic.Appointments.Services.AppointmentsService.Implementations;

public class AppointmentsService : IAppointmentsService
{
    private readonly IAppointmentsRepository _appointmentsRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly ITimeSlotsService _timeSlotsService;
    private readonly FilterResolver<Appointment> _filterResolver;
    private readonly BackgroundNotificationsService _notificationsService;
    private readonly IRequestClient<ProfileExistsAndHasRoleRequest> _profileConsistencyCheckClient;
    private readonly IRequestClient<ServiceExistsAndBelongsToSpecializationRequest> _serviceConsistencyCheckClient;


    public AppointmentsService(IRequestClient<ProfileExistsAndHasRoleRequest> profileConsistencyCheckClient,
        IRequestClient<ServiceExistsAndBelongsToSpecializationRequest> serviceConsistencyCheckClient,
        IAppointmentsRepository appointmentsRepository, IDoctorRepository doctorRepository,
        FilterResolver<Appointment> filterResolver, ITimeSlotsService timeSlotsService,
        BackgroundNotificationsService notificationsService)
    {
        _profileConsistencyCheckClient = profileConsistencyCheckClient;
        _serviceConsistencyCheckClient = serviceConsistencyCheckClient;
        _appointmentsRepository = appointmentsRepository;
        _doctorRepository = doctorRepository;
        _filterResolver = filterResolver;
        _timeSlotsService = timeSlotsService;
        _notificationsService = notificationsService;
    }

    public async Task<IEnumerable<ViewAppointmentHistoryDto>> GetPatientAppointmentHistory(Guid patientId)
    {
        var patientIdFilter = new PatientFilter().ToExpression(patientId.ToString());
        var appointments = await _appointmentsRepository.GetAppointmentsListingAsync(patientIdFilter);
        return appointments
            .OrderByDescending(x => x.ReservedTimeSlot.AppointmentStart)
            .ToAppointmentHistory();
    }

    public async Task<IEnumerable<AppointmentInfoDto>> GetDoctorsAppointmentsAsync(
        CompoundFilter<Appointment> filter)
    {
        var filterExpression = _filterResolver.ConvertCompoundFilterToExpression(filter);
        var appointments = await _appointmentsRepository.GetAppointmentsListingAsync(filterExpression);
        return appointments.ToDoctorAppointmentListing();
    }

    public async Task<IEnumerable<AppointmentInfoDto>> GetAppointmentsAsync(
        CompoundFilter<Appointment> filter)
    {
        var filterExpression = _filterResolver.ConvertCompoundFilterToExpression(filter);
        var appointments = await _appointmentsRepository.GetAppointmentsListingAsync(filterExpression);
        return appointments.ToAppointmentListing();
    }

    public async Task<Guid> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto)
    {
        await EnsureDataConsistency(createAppointmentDto);
        var reservedTimeSlot = await _timeSlotsService.TryReserveTimeSlot(
            createAppointmentDto.DoctorId,
            createAppointmentDto.AppointmentStart,
            createAppointmentDto.AppointmentFinish);

        var newAppointment = createAppointmentDto.ToNewAppointment(reservedTimeSlot);
        var appointmentId = await _appointmentsRepository.CreateAppointmentAsync(newAppointment);
        await _notificationsService.EnqueueNotification(new(NotificationType.AppointmentReminder, appointmentId,
            newAppointment.ReservedTimeSlot.AppointmentStart));
        return appointmentId;
    }

    public async Task<Guid> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto,
        Guid referrerPatientProfileId)
    {
        ValidatePatientPermissions(referrerPatientProfileId, createAppointmentDto.PatientId);
        return await CreateAppointmentAsync(createAppointmentDto);
    }

    public async Task UpdateAppointmentAsync(Guid appointmentId, AppointmentEditTimeAndStatusDto updatedAppointment)
    {
        var filterExpression = new IdFilter().ToExpression(appointmentId.ToString());
        var appointment = await _appointmentsRepository.GetAppointmentAsync(filterExpression);
        var isTimeSlotRebookingRequired =
            appointment.ReservedTimeSlot.AppointmentStart != updatedAppointment.AppointmentStart ||
            appointment.ReservedTimeSlot.AppointmentFinish != updatedAppointment.AppointmentEnd;

        if (isTimeSlotRebookingRequired)
        {
            var timeSlot = await _timeSlotsService.TryRebookTimeSlot(
                appointment.ReservedTimeSlotId,
                appointment.DoctorId,
                updatedAppointment.AppointmentStart,
                updatedAppointment.AppointmentEnd);
            appointment.ReservedTimeSlot = timeSlot;
        }

        appointment.Status = updatedAppointment.AppointmentStatus;
        await _appointmentsRepository.UpdateAppointmentAsync(appointment);
        await _notificationsService.EnqueueNotification(new(NotificationType.AppointmentReminder, appointmentId,
            appointment.ReservedTimeSlot.AppointmentStart));
    }

    public async Task UpdateAppointmentAsync(Guid id, AppointmentEditTimeDto updatedAppointment,
        Guid referrerPatientProfileId)
    {
        var appointmentIdFilter = new IdFilter().ToExpression(id.ToString());
        var appointment = await _appointmentsRepository.GetAppointmentAsync(appointmentIdFilter);
        ValidatePatientPermissions(referrerPatientProfileId, appointment.PatientId);
        var completeUpdateDto = updatedAppointment.ToCompleteUpdateDto(appointment);
        await UpdateAppointmentAsync(id, completeUpdateDto);
    }

    private async Task EnsureDataConsistency(CreateAppointmentDto createAppointmentDto)
    {
        var doctorIdFilterExpression =
            new Persistence.EntityFilters.Doctors.IdFilter().ToExpression(createAppointmentDto.DoctorId.ToString());
        var specializationIdFilterExpression =
            new SpecializationIdFilter().ToExpression(createAppointmentDto.SpecializationId.ToString());
        var officeIdFilterExpression = new OfficeIdFilter().ToExpression(createAppointmentDto.OfficeId.ToString());
        var complexFilter =
            doctorIdFilterExpression.And(specializationIdFilterExpression).And(officeIdFilterExpression);
        var doctorGetTask = _doctorRepository.GetDoctorAsync(complexFilter);
        var profileConsistencyCheckTask =
            _profileConsistencyCheckClient.GetResponse<ProfileExistsAndHasRoleResponse>(
                new(createAppointmentDto.PatientId, UserRoles.Patient));

        var serviceConsistencyCheckTask = _serviceConsistencyCheckClient
            .GetResponse<ServiceExistsAndBelongsToSpecializationResponse>(new(createAppointmentDto.ServiceId,
                createAppointmentDto.SpecializationId));

        var consistencyTasks = new List<Task>
            { doctorGetTask, profileConsistencyCheckTask, serviceConsistencyCheckTask };

        while (consistencyTasks.Any())
        {
            var finishedTask = await Task.WhenAny(consistencyTasks);

            if (finishedTask is Task<Response<ProfileExistsAndHasRoleResponse>> consistencyCheckTaskResult)
            {
                (await consistencyCheckTaskResult).Message.CheckIfDataIsConsistent();
            }

            else if (finishedTask is Task<Response<ServiceExistsAndBelongsToSpecializationResponse>>
                     serviceConsistencyCheckTaskResult)
            {
                (await serviceConsistencyCheckTaskResult).Message.CheckIfDataIsConsistent();
            }

            else if (finishedTask is Task<Doctor?> doctor)
            {
                if (await doctor is null)
                {
                    throw new InconsistentDataException(
                        "There is no doctor with requested id, specialization and office.");
                }
            }

            else
            {
                throw new ArgumentException("The returned task type is not supported.");
            }

            consistencyTasks.Remove(finishedTask);
        }
    }

    private void ValidatePatientPermissions(Guid referrerPatientProfileId, Guid targetEntityOwnerId)
    {
        if (targetEntityOwnerId != referrerPatientProfileId)
            throw new NotAllowedException("It is only possible to manage your own appointment.");
    }
}
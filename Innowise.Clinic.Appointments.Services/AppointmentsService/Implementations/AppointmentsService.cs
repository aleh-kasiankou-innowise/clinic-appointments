using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
using Innowise.Clinic.Shared.Constants;
using Innowise.Clinic.Shared.Enums;
using Innowise.Clinic.Shared.Exceptions;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Requests;
using MassTransit;

namespace Innowise.Clinic.Appointments.Services.AppointmentsService.Implementations;

public class AppointmentsService : IAppointmentsService
{
    // TODO ADD SORTING
    private readonly IAppointmentsRepository _appointmentsRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IRequestClient<TimeSlotReservationRequest> _timeslotReservationClient;
    private readonly IRequestClient<UpdateAppointmentTimeslotRequest> _timeslotUpdateClient;
    private readonly IRequestClient<ProfileExistsAndHasRoleRequest> _profileConsistencyCheckClient;
    private readonly IRequestClient<ServiceExistsAndBelongsToSpecializationRequest> _serviceConsistencyCheckClient;

    public AppointmentsService(IRequestClient<TimeSlotReservationRequest> reservationClient,
        IRequestClient<ProfileExistsAndHasRoleRequest> profileConsistencyCheckClient,
        IRequestClient<ServiceExistsAndBelongsToSpecializationRequest> serviceConsistencyCheckClient,
        IRequestClient<UpdateAppointmentTimeslotRequest> timeslotUpdateClient,
        IAppointmentsRepository appointmentsRepository, IDoctorRepository doctorRepository)
    {
        _timeslotReservationClient = reservationClient;
        _profileConsistencyCheckClient = profileConsistencyCheckClient;
        _serviceConsistencyCheckClient = serviceConsistencyCheckClient;
        _timeslotUpdateClient = timeslotUpdateClient;
        _appointmentsRepository = appointmentsRepository;
        _doctorRepository = doctorRepository;
    }

    public async Task<IEnumerable<ViewAppointmentHistoryDto>> GetPatientAppointmentHistory(Guid patientId)
    {
        var appointments = await _appointmentsRepository.GetAppointmentsListingAsync(x => x.PatientId == patientId);

        return appointments.OrderByDescending(x => x.ReservedTimeSlot.AppointmentStart).Select(x =>
            new ViewAppointmentHistoryDto
            {
                AppointmentId = x.AppointmentId,
                AppointmentStart = x.ReservedTimeSlot.AppointmentStart,
                AppointmentFinish = x.ReservedTimeSlot.AppointmentFinish,
                DoctorId = x.DoctorId,
                PatientId = x.PatientId,
                ServiceId = x.ServiceId,
                AppointmentResultId = x.AppointmentResultId
            });
    }

    public async Task<IEnumerable<AppointmentDoctorInfoDto>> GetDoctorsAppointmentsAsync(
        AppointmentDoctorFilterDto appointmentDoctorFilterDto)
    {
        var appointments = await _appointmentsRepository.GetAppointmentsListingAsync(x =>
            x.DoctorId == appointmentDoctorFilterDto.DoctorIdFilter && x.ReservedTimeSlot.AppointmentStart.Date ==
            appointmentDoctorFilterDto.DayFilter.Date);

        return appointments.Select(x => new AppointmentDoctorInfoDto
        {
            AppointmentId = x.AppointmentId,
            AppointmentStart = x.ReservedTimeSlot.AppointmentStart,
            AppointmentFinish = x.ReservedTimeSlot.AppointmentFinish,
            AppointmentStatus = x.Status,
            PatientId = x.PatientId,
            ServiceId = x.ServiceId,
            AppointmentResultId = x.AppointmentResultId
        });
    }

    public async Task<IEnumerable<AppointmentInfoDto>> GetAppointmentsAsync(
        AppointmentReceptionistFilterDto appointmentReceptionistFilterDto)
    {
        // use expression trees for filters
        /*if (appointmentReceptionistFilterDto.DoctorIdFilter != null)
        {
            query = query.Where(x => x.DoctorId == appointmentReceptionistFilterDto.DoctorIdFilter);
        }

        if (appointmentReceptionistFilterDto.StatusFilter != null)
        {
            query = query.Where(x => x.Status == appointmentReceptionistFilterDto.StatusFilter);
        }

        if (appointmentReceptionistFilterDto.ServiceIdFilter != null)
        {
            query = query.Where(x => x.ServiceId == appointmentReceptionistFilterDto.ServiceIdFilter);
        }

        if (appointmentReceptionistFilterDto.OfficeIdFilter != null)
        {
            query = query.Where(x => x.Doctor.OfficeId == appointmentReceptionistFilterDto.OfficeIdFilter);
        }*/
        
        Expression<Func<Appointment, bool>> filter = x => true;
        var appointments = await _appointmentsRepository.GetAppointmentsListingAsync(x =>
            x.ReservedTimeSlot.AppointmentStart.Date == appointmentReceptionistFilterDto.DayFilter.Date);


        return appointments.Select(x => new AppointmentInfoDto
        {
            AppointmentId = x.AppointmentId,
            AppointmentStart = x.ReservedTimeSlot.AppointmentStart,
            AppointmentFinish = x.ReservedTimeSlot.AppointmentFinish,
            AppointmentStatus = x.Status,
            PatientId = x.PatientId,
            ServiceId = x.ServiceId
        });
    }

    public async Task<Guid> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto)
    {
        await EnsureDataConsistency(createAppointmentDto);
        var timeSlotReservationResult = await _timeslotReservationClient.GetResponse<TimeSlotReservationResponse>(new(
            createAppointmentDto.DoctorId,
            createAppointmentDto.AppointmentStart,
            createAppointmentDto.AppointmentFinish)
        );

        if (timeSlotReservationResult.Message is { IsSuccessful: true, ReservedTimeSlotId: { } })
        {
            var newTimeSlotReservation = new ReservedTimeSlot
            {
                ReservedTimeSlotId = (Guid)timeSlotReservationResult.Message.ReservedTimeSlotId,
                AppointmentStart = createAppointmentDto.AppointmentStart,
                AppointmentFinish = createAppointmentDto.AppointmentFinish
            };
            var newAppointment = new Appointment
            {
                DoctorId = createAppointmentDto.DoctorId,
                ServiceId = createAppointmentDto.ServiceId,
                PatientId = createAppointmentDto.PatientId,
                Status = AppointmentStatus.Created,
                ReservedTimeSlot = newTimeSlotReservation,
            };

            await _appointmentsRepository.CreateAppointmentAsync(newAppointment);
            return newAppointment.AppointmentId;
        }

        throw new ReservationFailedException(timeSlotReservationResult.Message.FailReason ??
                                             throw new MissingErrorMessageException());
    }

    public async Task<Guid> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto,
        Guid referrerPatientProfileId)
    {
        if (createAppointmentDto.PatientId != referrerPatientProfileId)
            throw new NotAllowedException("It is only possible to make an appointment for yourself.");

        return await CreateAppointmentAsync(createAppointmentDto);
    }

    public async Task UpdateAppointmentAsync(Guid id, AppointmentEditTimeAndStatusDto updatedAppointment)
    {
        var appointment = await _appointmentsRepository.GetAppointmentAsync(id);
        if (appointment.ReservedTimeSlot.AppointmentStart != updatedAppointment.AppointmentStart &&
            appointment.ReservedTimeSlot.AppointmentFinish != updatedAppointment.AppointmentEnd)
        {
            var timeslotUpdateResponse = await _timeslotUpdateClient.GetResponse<UpdateAppointmentTimeslotResponse>(
                new(id, new(
                    updatedAppointment.DoctorId,
                    updatedAppointment.AppointmentStart,
                    updatedAppointment.AppointmentEnd)));

            if (!timeslotUpdateResponse.Message.IsSuccessful)
            {
                var message = timeslotUpdateResponse.Message.FailReason ??
                              throw new MissingErrorMessageException();
                throw new ReservationFailedException(message);
            }

            appointment.ReservedTimeSlot.AppointmentStart = updatedAppointment.AppointmentStart;
            appointment.ReservedTimeSlot.AppointmentFinish = updatedAppointment.AppointmentEnd;
        }

        appointment.Status = updatedAppointment.AppointmentStatus;
        await _appointmentsRepository.UpdateAppointmentAsync(appointment);
    }

    public async Task UpdateAppointmentAsync(Guid id, AppointmentEditTimeDto updatedAppointment,
        Guid referrerPatientProfileId)
    {
        // TODO AVOID DOUBLE DB CALL
        var appointment = await _appointmentsRepository.GetAppointmentAsync(id);
        if (appointment.PatientId != referrerPatientProfileId)
            throw new NotAllowedException("Patients are not allowed to edit appointments of other patients");
        await UpdateAppointmentAsync(id, new(
            id,
            updatedAppointment.DoctorId,
            updatedAppointment.AppointmentStart,
            updatedAppointment.AppointmentEnd,
            appointment.Status
        ));
    }

    private async Task EnsureDataConsistency(CreateAppointmentDto createAppointmentDto)
    {
        var doctorGetTask = _doctorRepository.GetDoctorAsync(x =>
            x.DoctorId == createAppointmentDto.DoctorId &&
            x.SpecializationId == createAppointmentDto.SpecializationId &&
            x.OfficeId == createAppointmentDto.OfficeId);

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
            if (finishedTask is Task<Response<ConsistencyCheckResponse>> consistencyCheckTask)
            {
                (await consistencyCheckTask).Message.CheckIfDataIsConsistent();
            }

            else if (finishedTask is Task<Doctor?> doctor && await doctor is null)
            {
                throw new InconsistentDataException(
                    "There is no doctor with requested id, specialization and office.");
            }

            else
            {
                throw new ArgumentException("The returned task type is not supported.");
            }

            consistencyTasks.Remove(finishedTask);
        }
    }
}
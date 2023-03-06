using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
using Innowise.Clinic.Shared.Exceptions;
using Innowise.Clinic.Shared.MassTransit.MessageTypes;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Innowise.Clinic.Appointments.Services.AppointmentsService.Implementations;

public class AppointmentsService : IAppointmentsService
{
    // TODO ADD SORTING

    private readonly AppointmentsDbContext _dbContext;
    private readonly IRequestClient<TimeSlotReservationRequest> _reservationClient;
    private readonly IRequestClient<ProfileExistsAndHasRoleRequest> _profileConsistencyCheckClient;
    private readonly IRequestClient<ServiceExistsAndBelongsToSpecializationRequest> _serviceConsistencyCheckClient;

    public AppointmentsService(AppointmentsDbContext dbContext,
        IRequestClient<TimeSlotReservationRequest> reservationClient,
        IRequestClient<ProfileExistsAndHasRoleRequest> profileConsistencyCheckClient,
        IRequestClient<ServiceExistsAndBelongsToSpecializationRequest> serviceConsistencyCheckClient)
    {
        _dbContext = dbContext;
        _reservationClient = reservationClient;
        _profileConsistencyCheckClient = profileConsistencyCheckClient;
        _serviceConsistencyCheckClient = serviceConsistencyCheckClient;
    }

    public async Task<IEnumerable<ViewAppointmentHistoryDto>> GetPatientAppointmentHistory(Guid patientId)
    {
        return await _dbContext.Appointments.Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.ReservedTimeSlot.AppointmentStart).Select(x => new ViewAppointmentHistoryDto
            {
                AppointmentId = x.AppointmentId,
                AppointmentStart = x.ReservedTimeSlot.AppointmentStart,
                AppointmentFinish = x.ReservedTimeSlot.AppointmentFinish,
                DoctorId = x.DoctorId,
                PatientId = x.PatientId,
                ServiceId = x.ServiceId,
                AppointmentResultId = x.AppointmentResultId
            }).ToListAsync();
    }

    public async Task<IEnumerable<AppointmentDoctorInfoDto>> GetDoctorsAppointmentsAsync(
        AppointmentDoctorFilterDto appointmentDoctorFilterDto)
    {
        return await _dbContext.Appointments.Where(x =>
                x.DoctorId == appointmentDoctorFilterDto.DoctorIdFilter && x.ReservedTimeSlot.AppointmentStart.Date ==
                appointmentDoctorFilterDto.DayFilter.Date)
            .Select(x => new AppointmentDoctorInfoDto
            {
                AppointmentId = x.AppointmentId,
                AppointmentStart = x.ReservedTimeSlot.AppointmentStart,
                AppointmentFinish = x.ReservedTimeSlot.AppointmentFinish,
                AppointmentStatus = x.Status,
                PatientId = x.PatientId,
                ServiceId = x.ServiceId,
                AppointmentResultId = x.AppointmentResultId
            }).ToListAsync();
    }

    public async Task<IEnumerable<AppointmentInfoDto>> GetAppointmentsAsync(
        AppointmentReceptionistFilterDto appointmentReceptionistFilterDto)
    {
        var query = _dbContext.Appointments.Where(x =>
            x.ReservedTimeSlot.AppointmentStart.Date == appointmentReceptionistFilterDto.DayFilter.Date);

        if (appointmentReceptionistFilterDto.DoctorIdFilter != null)
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
        }

        return await query.Select(x => new AppointmentInfoDto
        {
            AppointmentId = x.AppointmentId,
            AppointmentStart = x.ReservedTimeSlot.AppointmentStart,
            AppointmentFinish = x.ReservedTimeSlot.AppointmentFinish,
            AppointmentStatus = x.Status,
            PatientId = x.PatientId,
            ServiceId = x.ServiceId
        }).ToListAsync();
    }

    public async Task<Guid> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto)
    {
        await EnsureDataConsistency(createAppointmentDto);
        var timeSlotReservationResult = await _reservationClient.GetResponse<TimeSlotReservationResponse>(new(
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
            await _dbContext.ReservedTimeSlots.AddAsync(newTimeSlotReservation);
            await _dbContext.Appointments.AddAsync(newAppointment);
            await _dbContext.SaveChangesAsync();
            return newAppointment.AppointmentId;
        }

        throw new ReservationFailedException(timeSlotReservationResult.Message.FailReason ??
                                             throw new ArgumentException("No fail reason provided",
                                                 "createAppointmentDto"));
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
        var appointment = await GetAppointment(id);
        // TODO CALL TIMESLOT SERVICE TO RESERVE TIMESLOT
    }

    public async Task UpdateAppointmentAsync(Guid id, AppointmentEditTimeDto updatedAppointment,
        Guid referrerPatientProfileId)
    {
        var appointment = await GetAppointment(id);
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

    private async Task<Appointment> GetAppointment(Guid id)
    {
        return await _dbContext.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == id) ??
               throw new EntityNotFoundException("The requested appointment doesn't exist");
    }

    private async Task EnsureDataConsistency(CreateAppointmentDto createAppointmentDto)
    {
        var doctorGetTask = _dbContext.Doctors.FirstOrDefaultAsync(x =>
            x.DoctorId == createAppointmentDto.DoctorId &&
            x.SpecializationId == createAppointmentDto.SpecializationId &&
            x.OfficeId == createAppointmentDto.OfficeId);

        var profileConsistencyCheckTask =
            _profileConsistencyCheckClient.GetResponse<ProfileExistsAndHasRoleResponse>(
                new(createAppointmentDto.PatientId, "Patient"));

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
using System.Net.Http.Json;
using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
using Innowise.Clinic.Appointments.Services.MassTransitService.MessageTypes;
using Microsoft.EntityFrameworkCore;

namespace Innowise.Clinic.Appointments.Services.AppointmentsService.Implementations;

public class AppointmentsService : IAppointmentsService
{
    // TODO ADD SORTING

    private readonly AppointmentsDbContext _dbContext;

    public AppointmentsService(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
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

        // TODO MAKE REQUEST TO SCHEDULING SERVICE. The message will be sent to save reservation here as well;
        var timeSlotReservationResult = new TimeSlotReservationResponse(default, default);
        if (timeSlotReservationResult is { IsSuccessful: true, ReservedTimeSlotId: { } })
        {
            var newTimeSlotReservation = new ReservedTimeSlot
            {
                ReservedTimeSlotId = (Guid)timeSlotReservationResult.ReservedTimeSlotId,
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

        throw new NotImplementedException("Exception saying the reservation failed, + specify reason.");
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
        // TODO CALL TIMESLOT SERVICE TO RESERVE TIMESLOT
    }

    private async Task<Appointment> GetAppointment(Guid id)
    {
        return await _dbContext.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == id) ??
               throw new EntityNotFoundException("The requested appointment doesn't exist");
    }

    private async Task EnsureDataConsistency(CreateAppointmentDto createAppointmentDto)
    {
        var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(x =>
                         x.DoctorId == createAppointmentDto.DoctorId &&
                         x.SpecializationId == createAppointmentDto.SpecializationId &&
                         x.OfficeId == createAppointmentDto.OfficeId)
                     ?? throw new InconsistentDataException(
                         "There is no doctor with such id, specialization and office.");

        //TODO GROUP TASKS AND CHECK IF ANY IS FALSE
        var httpClient = new HttpClient();
        //TODO convert to MassTransit requests
        var patientConsistencyCheck = await httpClient.PostAsJsonAsync(
            "http://profile:80/helperservices/ensure-created",
            new ProfileConsistencyCheckDto
            {
                ProfileId = createAppointmentDto.PatientId,
                Role = "Patient"
            });
        if (!patientConsistencyCheck.IsSuccessStatusCode)
            throw new InconsistentDataException(
                "The requested patient doesn't exist.");

        //TODO convert to MassTransit requests
        var serviceAndSpecializationConsistencyCheck =
            await httpClient.GetAsync(
                $"http://service:80/helperservices/ensure-exists/service/{createAppointmentDto.ServiceId}?specializationId={createAppointmentDto.SpecializationId}");
        if (!serviceAndSpecializationConsistencyCheck.IsSuccessStatusCode)
            throw new InconsistentDataException(
                "The requested service either doesn't exist or belongs to a different specialization.");
    }
}
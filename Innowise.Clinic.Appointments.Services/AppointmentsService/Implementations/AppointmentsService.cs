using System.Net.Http.Json;
using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
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
            query = query.Where(x => x.OfficeId == appointmentReceptionistFilterDto.OfficeIdFilter);
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

        var newTimeSlotReservation = new ReservedTimeSlot
        {
            AppointmentStart = createAppointmentDto.AppointmentStart,
            AppointmentFinish = createAppointmentDto.AppointmentFinish
        };

        var newAppointment = new Appointment
        {
            DoctorId = createAppointmentDto.DoctorId,
            SpecializationId = createAppointmentDto.SpecializationId,
            ServiceId = createAppointmentDto.ServiceId,
            OfficeId = createAppointmentDto.OfficeId,
            PatientId = createAppointmentDto.PatientId,
            Status = AppointmentStatus.Created,
            ReservedTimeSlot = newTimeSlotReservation,
        };

        await _dbContext.ReservedTimeSlots.AddAsync(newTimeSlotReservation);
        await _dbContext.Appointments.AddAsync(newAppointment);
        await _dbContext.SaveChangesAsync();

        return newAppointment.AppointmentId;
    }


    public async Task UpdateAppointmentAsync(Guid id, AppointmentEditDto updatedAppointment)
    {
        //TODO IF SENT BY PATIENT, CHECK THAT APPOINTMENT PATIENT ID IS EQUAL TO SENDER PATIENT ID
        var appointment = await _dbContext.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == id) ??
                          throw new EntityNotFoundException("The requested appointment doesn't exist");

        // TODO ADD ANOTHER METHOD FOR DIFFERENT ROLES 
    }

    private async Task EnsureDataConsistency(CreateAppointmentDto createAppointmentDto)
    {
        //TODO GROUP TASKS AND CHECK IF ANY IS FALSE

        var httpClient = new HttpClient();
        var doctorConsistencyCheck = await httpClient.PostAsJsonAsync("http://profile:80/helperservices/ensure-created",
            new ProfileConsistencyCheckDto
            {
                ProfileId = createAppointmentDto.DoctorId,
                Role = "Doctor",
                SpecializationId = createAppointmentDto.SpecializationId,
                OfficeId = createAppointmentDto.OfficeId
            });
        if (!doctorConsistencyCheck.IsSuccessStatusCode)
            throw new InconsistentDataException(
                "The requested doctor either doesn't exist or has a different specialization.");

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

        var officeConsistencyCheck =
            await httpClient.GetAsync(
                $"http://office:80/helperservices/ensure-exists/office/{createAppointmentDto.OfficeId}");
        if (!officeConsistencyCheck.IsSuccessStatusCode)
            throw new InconsistentDataException(
                "The requested office doesn't exist.");

        var serviceAndSpecializationConsistencyCheck =
            await httpClient.GetAsync(
                $"http://service:80/helperservices/ensure-exists/service/{createAppointmentDto.ServiceId}?specializationId={createAppointmentDto.SpecializationId}");
        if (!serviceAndSpecializationConsistencyCheck.IsSuccessStatusCode)
            throw new InconsistentDataException(
                "The requested service either doesn't exist or belongs to a different specialization.");
    }
}
using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Persistence;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Innowise.Clinic.Appointments.Services.AppointmentResultsService.Implementations;

public class AppointmentResultsService : IAppointmentResultsService
{
    private readonly AppointmentsDbContext _dbContext;

    public AppointmentResultsService(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<ViewAppointmentResultDto> GetDoctorAppointmentResult(Guid id, Guid doctorId)
    {
        var appointmentResult = await GetAppointmentResultByDoctorId(id, doctorId);

        return new ViewAppointmentResultDto
        {
            AppointmentResultId = appointmentResult.AppointmentResultId,
            AppointmentDate = appointmentResult.Appointment.ReservedTimeSlot.AppointmentStart.Date,
            PatientId = appointmentResult.Appointment.PatientId,
            DoctorId = doctorId,
            SpecializationId = appointmentResult.Appointment.SpecializationId,
            ServiceId = appointmentResult.Appointment.ServiceId,
            Complaints = appointmentResult.Complaints,
            Conclusion = appointmentResult.Conclusion,
            Recommendations = appointmentResult.Recommendations
        };
    }

    public async Task<ViewAppointmentResultDto> GetPatientAppointmentResult(Guid id, Guid patientId)
    {
        var appointmentResult = await GetAppointmentResultByPatientId(id, patientId);

        return new ViewAppointmentResultDto
        {
            AppointmentResultId = appointmentResult.AppointmentResultId,
            AppointmentDate = appointmentResult.Appointment.ReservedTimeSlot.AppointmentStart.Date,
            PatientId = appointmentResult.Appointment.PatientId,
            DoctorId = appointmentResult.Appointment.DoctorId,
            SpecializationId = appointmentResult.Appointment.SpecializationId,
            ServiceId = appointmentResult.Appointment.ServiceId,
            Complaints = appointmentResult.Complaints,
            Conclusion = appointmentResult.Conclusion,
            Recommendations = appointmentResult.Recommendations
        };
    }

    public async Task<Guid> CreateAppointmentResult(CreateAppointmentResultDto newAppointmentResult, Guid doctorId)
    {
        var appointment = await _dbContext.Appointments.FirstOrDefaultAsync(a =>
                              a.AppointmentId == newAppointmentResult.AppointmentId && a.DoctorId == doctorId) ??
                          throw new NotImplementedException();

        var appointmentResult = new AppointmentResult
        {
            Complaints = newAppointmentResult.Complaints,
            Conclusion = newAppointmentResult.Conclusion,
            Recommendations = newAppointmentResult.Recommendations
        };

        appointment.AppointmentResult = appointmentResult;

        _dbContext.AppointmentResults.Add(appointmentResult);
        _dbContext.Update(appointment);

        await _dbContext.SaveChangesAsync();

        return appointmentResult.AppointmentResultId;
    }

    public async Task UpdateAppointmentResult(Guid id, AppointmentResultEditDto updatedAppointmentResult, Guid doctorId)
    {
        var appointment = await GetAppointmentResultByDoctorId(id, doctorId);
        appointment.Complaints = updatedAppointmentResult.Complaints;
        appointment.Conclusion = updatedAppointmentResult.Conclusion;
        appointment.Recommendations = updatedAppointmentResult.Recommendations;

        _dbContext.Update(appointment);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<AppointmentResult> GetAppointmentResultByDoctorId(Guid id, Guid doctorId)
    {
        return await _dbContext.AppointmentResults.Include(ar => ar.Appointment).ThenInclude(a => a.ReservedTimeSlot)
                   .FirstOrDefaultAsync(x =>
                       x.AppointmentResultId == id && x.Appointment.DoctorId == doctorId) ??
               throw new NotImplementedException("The requested appointment doesn't exist");
    }

    private async Task<AppointmentResult> GetAppointmentResultByPatientId(Guid id, Guid patientId)
    {
        return await _dbContext.AppointmentResults.Include(ar => ar.Appointment).ThenInclude(a => a.ReservedTimeSlot)
                   .FirstOrDefaultAsync(x =>
                       x.AppointmentResultId == id && x.Appointment.PatientId == patientId) ??
               throw new NotImplementedException("The requested appointment doesn't exist");
    }
}
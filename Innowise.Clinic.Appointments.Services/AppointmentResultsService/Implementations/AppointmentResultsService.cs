using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Persistence;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using Innowise.Clinic.Shared.Exceptions;
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
            SpecializationId = appointmentResult.Appointment.Doctor.SpecializationId,
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
            SpecializationId = appointmentResult.Appointment.Doctor.SpecializationId,
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
                          throw new EntityNotFoundException("The requested appointment doesn't exist.");

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
        // TODO MAKE METHODS ADHERE TO DRY PRINCIPLE
        return await _dbContext.AppointmentResults
                   .Include(ar => ar.Appointment)
                   .ThenInclude(a => a.ReservedTimeSlot)
                   .Include(x => x.Appointment)
                   .ThenInclude(app => app.Doctor)
                   .FirstOrDefaultAsync(x =>
                       x.AppointmentResultId == id && x.Appointment.DoctorId == doctorId) ??
               throw new EntityNotFoundException("The requested appointment doesn't exist.");
    }

    private async Task<AppointmentResult> GetAppointmentResultByPatientId(Guid id, Guid patientId)
    {
        // TODO MAKE METHODS ADHERE TO DRY PRINCIPLE
        return await _dbContext.AppointmentResults
                   .Include(ar => ar.Appointment)
                   .ThenInclude(a => a.ReservedTimeSlot)
                   .Include(x => x.Appointment)
                   .ThenInclude(app => app.Doctor)
                   .FirstOrDefaultAsync(x =>
                       x.AppointmentResultId == id && x.Appointment.PatientId == patientId) ??
               throw new EntityNotFoundException("The requested appointment doesn't exist.");
    }
}
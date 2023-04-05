using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using MassTransit;

namespace Innowise.Clinic.Appointments.Services.AppointmentResultsService.Implementations;

public class AppointmentResultsService : IAppointmentResultsService
{
    private readonly IAppointmentResultsRepository _appointmentResultsRepository;
    private readonly IBus _bus;

    public AppointmentResultsService(IBus bus, IAppointmentResultsRepository appointmentResultsRepository)
    {
        _bus = bus;
        _appointmentResultsRepository = appointmentResultsRepository;
    }

    public async Task<ViewAppointmentResultDto> GetDoctorAppointmentResult(Guid id, Guid doctorId)
    {
        // todo remove hardcoded expression
        var appointmentResult = await _appointmentResultsRepository.GetAppointmentResultAsync(x =>
            x.AppointmentResultId == id && x.Appointment.DoctorId == doctorId);

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
        // todo remove hardcoded expression
        var appointmentResult = await _appointmentResultsRepository.GetAppointmentResultAsync(x =>
            x.AppointmentResultId == id && x.Appointment.PatientId == patientId);
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
        var appointmentResult = new AppointmentResult
        {
            Complaints = newAppointmentResult.Complaints,
            Conclusion = newAppointmentResult.Conclusion,
            Recommendations = newAppointmentResult.Recommendations,
            AppointmentId = newAppointmentResult.AppointmentId
        };

        await _appointmentResultsRepository.CreateAppointmentResultAsync(appointmentResult);

        // need patient email and full appointment info
        // todo send event with requested info

        return appointmentResult.AppointmentResultId;
    }

    public async Task UpdateAppointmentResult(Guid id, AppointmentResultEditDto updatedAppointmentResult, Guid doctorId)
    {
        var appointmentResult = new AppointmentResult
        {
            AppointmentResultId = id,
            Complaints = updatedAppointmentResult.Complaints,
            Conclusion = updatedAppointmentResult.Conclusion,
            Recommendations = updatedAppointmentResult.Recommendations
        };

        await _appointmentResultsRepository.UpdateAppointmentResultAsync(appointmentResult);

        // todo send event with requested info
    }
}
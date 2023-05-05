using Innowise.Clinic.Appointments.Dto;

namespace Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;

public interface IAppointmentResultsService
{
    Task<ViewAppointmentResultDto> GetDoctorAppointmentResult(Guid appointmentId, Guid doctorId);
    Task<ViewAppointmentResultDto> GetPatientAppointmentResult(Guid appointmentId, Guid patientId);

    Task<Guid> CreateAppointmentResult(CreateAppointmentResultDto newAppointmentResult);

    Task UpdateAppointmentResult(Guid id, AppointmentResultEditDto updatedAppointmentResult);
}
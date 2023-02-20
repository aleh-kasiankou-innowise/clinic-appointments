using Innowise.Clinic.Appointments.Dto;

namespace Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;

public interface IAppointmentResultsService
{
    Task<ViewAppointmentResultDto> GetDoctorAppointmentResult(Guid id, Guid doctorId);
    Task<ViewAppointmentResultDto> GetPatientAppointmentResult(Guid id, Guid patientId);

    Task<Guid> CreateAppointmentResult(CreateAppointmentResultDto newAppointmentResult, Guid doctorId);

    Task UpdateAppointmentResult(Guid id, AppointmentResultEditDto updatedAppointmentResult, Guid doctorId);
}
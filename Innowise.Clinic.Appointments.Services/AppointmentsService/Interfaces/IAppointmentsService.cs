using Innowise.Clinic.Appointments.Dto;

namespace Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;

public interface IAppointmentsService
{
    Task<IEnumerable<ViewAppointmentHistoryDto>> GetPatientAppointmentHistory(Guid patientId);

    Task<IEnumerable<AppointmentDoctorInfoDto>> GetDoctorsAppointmentsAsync(
        AppointmentDoctorFilterDto appointmentDoctorFilterDto);

    Task<IEnumerable<AppointmentInfoDto>> GetAppointmentsAsync(
        AppointmentReceptionistFilterDto appointmentReceptionistFilterDto);

    Task<Guid> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto);

    Task UpdateAppointmentAsync(Guid id, AppointmentEditDto updatedAppointment);
}
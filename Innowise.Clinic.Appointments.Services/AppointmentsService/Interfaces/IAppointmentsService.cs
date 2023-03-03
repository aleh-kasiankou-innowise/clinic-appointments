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

    Task<Guid> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto, Guid referrerPatientProfileId);

    Task UpdateAppointmentAsync(Guid id, AppointmentEditTimeAndStatusDto updatedAppointment);
    Task UpdateAppointmentAsync(Guid id, AppointmentEditTimeDto updatedAppointment, Guid referrerPatientProfileId);
}
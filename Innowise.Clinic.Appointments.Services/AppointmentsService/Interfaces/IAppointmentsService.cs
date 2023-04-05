using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;

namespace Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;

public interface IAppointmentsService
{
    Task<IEnumerable<ViewAppointmentHistoryDto>> GetPatientAppointmentHistory(Guid patientId);

    Task<IEnumerable<AppointmentDoctorInfoDto>> GetDoctorsAppointmentsAsync(
        CompoundFilter<Appointment> appointmentDoctorFilterDto);

    Task<IEnumerable<AppointmentInfoDto>> GetAppointmentsAsync(
        CompoundFilter<Appointment> appointmentReceptionistFilterDto);

    Task<Guid> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto);

    Task<Guid> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto, Guid referrerPatientProfileId);

    Task UpdateAppointmentAsync(Guid id, AppointmentEditTimeAndStatusDto updatedAppointment);
    Task UpdateAppointmentAsync(Guid id, AppointmentEditTimeDto updatedAppointment, Guid referrerPatientProfileId);
}
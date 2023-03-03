using Innowise.Clinic.Appointments.Persistence.Models;

namespace Innowise.Clinic.Appointments.Dto;

public record AppointmentEditTimeDto(Guid AppointmentId, Guid DoctorId, DateTime AppointmentStart,
    DateTime AppointmentEnd);

public record AppointmentEditTimeAndStatusDto(
        Guid AppointmentId,
        Guid DoctorId,
        DateTime AppointmentStart,
        DateTime AppointmentEnd,
        AppointmentStatus AppointmentStatus)
    : AppointmentEditTimeDto(
        AppointmentId,
        DoctorId,
        AppointmentStart,
        AppointmentEnd);
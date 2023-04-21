using Innowise.Clinic.Shared.Enums;

namespace Innowise.Clinic.Appointments.Dto;

public class AppointmentInfoDto
{
    public Guid AppointmentId { get; set; }
    public DateTime AppointmentStart { get; set; }
    public DateTime AppointmentFinish { get; set; }
    public AppointmentStatus AppointmentStatus { get; set; }
    public Guid PatientId { get; set; }
    public Guid ServiceId { get; set; }
}

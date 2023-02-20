using Innowise.Clinic.Appointments.Persistence.Models;

namespace Innowise.Clinic.Appointments.Dto;



public abstract class AppointmentEditDto
{
    public Guid AppointmentId { get; set; }
}

public class AppointmentEditStatusDto : AppointmentEditDto
{
    public AppointmentStatus AppointmentStatus { get; set; }
}

public class AppointmentEditTimeDto : AppointmentEditDto
{
    public Guid DoctorId { get; set; }
    public Guid AppointmentStart { get; init; }
}

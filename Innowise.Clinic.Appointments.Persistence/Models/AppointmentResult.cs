using Innowise.Clinic.Shared.BaseClasses;

namespace Innowise.Clinic.Appointments.Persistence.Models;

public class AppointmentResult : IEntity
{
    public Guid AppointmentResultId { get; set; }
    public Guid AppointmentId { get; set; }
    public virtual Appointment Appointment { get; set; }
    public string Complaints { get; set; }
    public string Conclusion { get; set; }
    public string Recommendations { get; set; }
}
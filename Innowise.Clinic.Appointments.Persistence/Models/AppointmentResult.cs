namespace Innowise.Clinic.Appointments.Persistence.Models;

public class AppointmentResult
{
    public Guid AppointmentResultId { get; set; }
    public virtual Appointment Appointment { get; set; }
    public string Complaints { get; set; }
    public string Conclusion { get; set; }
    public string Recommendations { get; set; }
}
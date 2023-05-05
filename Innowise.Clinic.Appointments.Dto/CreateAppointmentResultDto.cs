namespace Innowise.Clinic.Appointments.Dto;

public class CreateAppointmentResultDto
{
    public Guid AppointmentId { get; set; }
    public string Complaints { get; set; }
    public string Conclusion { get; set; }
    public string Recommendations { get; set; }
}
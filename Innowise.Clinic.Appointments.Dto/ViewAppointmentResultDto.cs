namespace Innowise.Clinic.Appointments.Dto;

public class ViewAppointmentResultDto
{
    public Guid AppointmentResultId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid SpecializationId { get; set; }
    public Guid ServiceId { get; set; }
    public string Complaints { get; set; }
    public string Conclusion { get; set; }
    public string Recommendations { get; set; }
    public string? PdfResultsLink { get; set; }
}
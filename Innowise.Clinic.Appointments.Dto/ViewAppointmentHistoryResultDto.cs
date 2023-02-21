namespace Innowise.Clinic.Appointments.Dto;

public class ViewAppointmentHistoryDto
{
    public Guid AppointmentId { get; set; }
    public DateTime AppointmentStart { get; set; }
    public DateTime AppointmentFinish { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? AppointmentResultId { get; set; }
}
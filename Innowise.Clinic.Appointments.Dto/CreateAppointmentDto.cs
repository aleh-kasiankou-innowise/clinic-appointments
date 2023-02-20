namespace Innowise.Clinic.Appointments.Dto;

public class CreateAppointmentDto
{
    public Guid SpecializationId { get; init; }
    public Guid DoctorId { get; init; }
    public Guid PatientId { get; init; }
    public Guid ServiceId { get; init; }
    public Guid OfficeId { get; init; }
    public DateTime AppointmentStart { get; init; }
    public DateTime AppointmentFinish { get; init; }
}

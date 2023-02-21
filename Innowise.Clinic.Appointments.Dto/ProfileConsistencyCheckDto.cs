namespace Innowise.Clinic.Appointments.Dto;

public class ProfileConsistencyCheckDto
{
    public Guid ProfileId { get; set; }
    public string Role { get; set; }
    public Guid? SpecializationId { get; set; }
    public Guid? OfficeId { get; set; }
}
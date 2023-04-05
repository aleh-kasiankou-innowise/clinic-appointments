using Innowise.Clinic.Shared.BaseClasses;

namespace Innowise.Clinic.Appointments.Persistence.Models;

public class Doctor : IEntity
{
    public Guid DoctorId { get; set; }
    public Guid SpecializationId { get; set; }
    public Guid OfficeId { get; set; }
}
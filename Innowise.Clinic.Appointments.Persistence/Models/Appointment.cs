using Innowise.Clinic.Shared.BaseClasses;
using Innowise.Clinic.Shared.Enums;

namespace Innowise.Clinic.Appointments.Persistence.Models;

public class Appointment : IEntity
{
    public Guid AppointmentId { get; set; }
    public Guid DoctorId { get; set; }
    public virtual  Doctor Doctor { get; set; }
    public Guid ServiceId { get; set; }
    public Guid PatientId { get; set; }
    public AppointmentStatus Status { get; set; }
    public Guid ReservedTimeSlotId { get; set; }
    public virtual ReservedTimeSlot ReservedTimeSlot { get; set; }
}
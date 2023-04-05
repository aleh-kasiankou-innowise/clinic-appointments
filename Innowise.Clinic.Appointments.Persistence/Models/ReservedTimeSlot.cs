using Innowise.Clinic.Shared.BaseClasses;

namespace Innowise.Clinic.Appointments.Persistence.Models;

public class ReservedTimeSlot : IEntity, IEquatable<ReservedTimeSlot>
{
    public Guid ReservedTimeSlotId { get; set; }
    public virtual Appointment Appointment { get; set; }
    public DateTime AppointmentStart { get; set; }
    public DateTime AppointmentFinish { get; set; }

    public bool Equals(ReservedTimeSlot? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ReservedTimeSlotId.Equals(other.ReservedTimeSlotId) &&
               AppointmentStart.Equals(other.AppointmentStart) && 
               AppointmentFinish.Equals(other.AppointmentFinish);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ReservedTimeSlot)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReservedTimeSlotId, AppointmentStart, AppointmentFinish);
    }
}
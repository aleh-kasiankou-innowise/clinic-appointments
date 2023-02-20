namespace Innowise.Clinic.Appointments.Persistence.Models;

public class ReservedTimeSlot
{
    public Guid ReservedTimeSlotId { get; set; }
    public Guid AppointmentId { get; set; }
    public DateTime AppointmentStart { get; set; }
    public DateTime AppointmentFinish { get; set; }

}
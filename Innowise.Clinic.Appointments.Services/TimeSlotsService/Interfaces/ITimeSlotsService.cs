using Innowise.Clinic.Appointments.Persistence.Models;

namespace Innowise.Clinic.Appointments.Services.TimeSlotsService.Interfaces;

public interface ITimeSlotsService
{
    Task<ReservedTimeSlot> TryReserveTimeSlot(Guid doctorId, DateTime appointmentStart,
        DateTime appointmentFinish);

    Task<ReservedTimeSlot> TryRebookTimeSlot(Guid timeSlotId, Guid doctorId, DateTime appointmentStart,
        DateTime appointmentFinish);
}
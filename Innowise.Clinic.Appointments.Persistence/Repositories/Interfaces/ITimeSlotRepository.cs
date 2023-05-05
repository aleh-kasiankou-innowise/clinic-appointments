using Innowise.Clinic.Appointments.Persistence.Models;
using Npgsql;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;

public interface ITimeSlotRepository
{
    Task<Guid> ReserveTimeSlot(NpgsqlConnection connection, ReservedTimeSlot newTimeSlot);
    Task RemoveReservation(NpgsqlConnection connection, Guid timeSlotId);
}
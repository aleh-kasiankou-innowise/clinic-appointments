namespace Innowise.Clinic.Appointments.Services.MassTransitService.MessageTypes;

public record TimeSlotReservationRequest(DateTime AppointmentStart, DateTime AppointmentEnd);
public record TimeSlotReservationResponse(bool IsSuccessful, Guid? ReservedTimeSlotId);
namespace Innowise.Clinic.Appointments.Services.MassTransitService.MessageTypes;

public record TimeSlotReservationRequest(Guid DoctorId, DateTime AppointmentStart, DateTime AppointmentEnd);

public record TimeSlotReservationResponse(bool IsSuccessful, Guid? ReservedTimeSlotId);
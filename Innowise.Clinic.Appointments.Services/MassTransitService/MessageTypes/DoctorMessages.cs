namespace Innowise.Clinic.Appointments.Services.MassTransitService.MessageTypes;

public record DoctorAddedMessage(Guid DoctorId, Guid SpecializationId, Guid OfficeId);

public record DoctorUpdatedMessage(Guid DoctorId, Guid SpecializationId, Guid OfficeId);

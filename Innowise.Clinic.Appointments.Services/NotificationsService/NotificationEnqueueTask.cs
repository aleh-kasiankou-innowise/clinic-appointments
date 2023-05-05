namespace Innowise.Clinic.Appointments.Services.NotificationsService;

public record NotificationTask(NotificationType NotificationType, Guid PrimaryId, DateTime? DeadLine = null);
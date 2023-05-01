using System.Collections.Concurrent;
using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Appointments.Services.Mappings;
using Innowise.Clinic.Shared.Services.PredicateBuilder;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Innowise.Clinic.Appointments.Services.NotificationsService;

public class BackgroundNotificationsService : BackgroundService
{
    private readonly IBus _bus;
    private readonly IAppointmentsRepository _appointmentsRepository;
    private readonly IAppointmentResultsRepository _appointmentResultsRepository;
    private readonly TimeSpan _notificationSyncInterval;
    private readonly TimeSpan _notificationThresholdHoursBeforeAppointment;
    private readonly ConcurrentQueue<NotificationTask> _notificationsQueue = new();
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private DateTime _nextNotificationSync;
    private Timer _timer;


    public BackgroundNotificationsService(IBus bus, IAppointmentsRepository appointmentsRepository,
        IAppointmentResultsRepository appointmentResultsRepository)
    {
        var sendFrequency = Environment.GetEnvironmentVariable("AppointmentNotifications__SyncIntervalMinutes");
        var notifyHoursBeforeAppointment =
            Environment.GetEnvironmentVariable("AppointmentNotifications__SyncImmediatelyIfLessHoursBeforeAppointment");
        if (sendFrequency is null || notifyHoursBeforeAppointment is null)
        {
            throw new InvalidOperationException(
                "The environmental variables with notification configurations are not set");
        }

        _notificationSyncInterval =
            TimeSpan.FromMinutes(int.Parse(sendFrequency));
        _notificationThresholdHoursBeforeAppointment = TimeSpan.FromHours(int.Parse(notifyHoursBeforeAppointment));
        _nextNotificationSync = DateTime.Now + _notificationSyncInterval;
        _timer = new Timer(RunSynchronisation, null, _notificationSyncInterval, _notificationSyncInterval);

        _bus = bus;
        _appointmentsRepository = appointmentsRepository;
        _appointmentResultsRepository = appointmentResultsRepository;
    }

    public async Task EnqueueNotification(NotificationTask enqueueTask)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            // currently only appointment notifications have deadline
            if (enqueueTask.DeadLine is not null &&
                enqueueTask.DeadLine - _notificationThresholdHoursBeforeAppointment <=
                _nextNotificationSync)
            {
                var appointment = await _appointmentsRepository.GetAppointmentAsync(
                    new IdFilter().ToExpression(enqueueTask.PrimaryId.ToString()));
                await _bus.Publish(appointment.ToNotification());
                return;
            }

            _notificationsQueue.Enqueue(enqueueTask);
        }
        finally
        {
            _semaphoreSlim.Release();
            Log.Debug("Added notification to sync queue. Type: {MessageType}", enqueueTask.NotificationType);
        }
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
    }

    private void RunSynchronisation(object? state)
    {
        Log.Debug("Starting notification sync with notification service");
        _nextNotificationSync = DateTime.Now + _notificationSyncInterval;
        _ = SyncNotifications();
    }

    private async Task SyncNotifications()
    {
        List<NotificationTask> queueDump = new();
        (Expression<Func<Appointment, bool>> AppointmentBulkFilter,
            Expression<Func<AppointmentResult, bool>> AppointmentResultBulkFilter) bulkFilters =
                (x => false, y => false);
        try
        {
            await _semaphoreSlim.WaitAsync();
            queueDump = _notificationsQueue.ToList();
            bulkFilters = PrepareBulkFiltersForNotificationsDataRetrieval();
            _notificationsQueue.Clear();
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        try
        {
            // TODO SENDING TASKS WITHOUT AWAIT
            var appointmentsToNotifyOf =
                _appointmentsRepository.GetAppointmentsListingAsync(bulkFilters.AppointmentBulkFilter);
            var appointmentResultsToNotifyOf =
                _appointmentResultsRepository.GetAppointmentsListingAsync(bulkFilters.AppointmentResultBulkFilter);
            var tasks = new List<Task> { appointmentsToNotifyOf, appointmentResultsToNotifyOf };
            await PublishMessagesToBroker(tasks);
        }
        catch (Exception e)
        {
            Log.Warning("Cannot sync queue with notification service. Returning notifications back to queue");
            foreach (var notificationTask in queueDump)
            {
                _notificationsQueue.Enqueue(notificationTask);
            }

            throw;
        }
    }

    private (Expression<Func<Appointment, bool>> AppointmentBulkFilter, Expression<Func<AppointmentResult, bool>>
        AppointmentResultBulkFilter) PrepareBulkFiltersForNotificationsDataRetrieval()
    {
        var appointmentNotificationsToSyncIdFilter = _notificationsQueue
            .Where(x => x.NotificationType == NotificationType.AppointmentReminder)
            .Select(x => new IdFilter().ToExpression(x.PrimaryId.ToString()));

        var appointmentResultNotificationsToSyncIdFilter = _notificationsQueue
            .Where(x => x.NotificationType == NotificationType.AppointmentResultNotification)
            .Select(x =>
                new Persistence.EntityFilters.AppointmentResults.IdFilter().ToExpression(x.PrimaryId.ToString()));

        var bulkAppointmentFilter = appointmentNotificationsToSyncIdFilter
            .Aggregate((current, next) => current.Or(next));
        var bulkAppointmentResultFilter = appointmentResultNotificationsToSyncIdFilter
            .Aggregate((current, next) => current.Or(next));

        return (bulkAppointmentFilter, bulkAppointmentResultFilter);
    }

    private async Task PublishMessagesToBroker(List<Task> tasks)
    {
        var publishingTasks = new List<Task>();

        while (tasks.Any())
        {
            var accomplishedTask = await Task.WhenAny(tasks);
            tasks.Remove(accomplishedTask);

            if (accomplishedTask is Task<IEnumerable<Appointment>> appointmentBulkRetrievalTask)
            {
                foreach (var appointment in await appointmentBulkRetrievalTask)
                {
                    Log.Debug("Publishing Reminder for appointment with id {AppointmentId}", appointment.AppointmentId);
                    publishingTasks.Add(_bus.Publish(appointment.ToNotification()));
                }
            }
            else if (accomplishedTask is Task<IEnumerable<AppointmentResult>> appointmentResultBulkRetrievalTask)
            {
                foreach (var appointmentResult in await appointmentResultBulkRetrievalTask)
                {
                    Log.Debug("Publishing Result Notification for appointment with id {AppointmentId}",
                        appointmentResult.AppointmentId);
                    publishingTasks.Add(_bus.Publish(appointmentResult.ToNotification()));
                }
            }
        }

        await Task.WhenAll(publishingTasks);
    }
}
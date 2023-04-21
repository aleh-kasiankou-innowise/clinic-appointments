using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Innowise.Clinic.Appointments.Services.MassTransitService.Consumers;

public class DoctorChangesAppointmentsConsumer : IConsumer<DoctorAddedOrUpdatedMessage>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ILogger<DoctorChangesAppointmentsConsumer> _logger;

    public DoctorChangesAppointmentsConsumer(IDoctorRepository doctorRepository, ILogger<DoctorChangesAppointmentsConsumer> logger)
    {
        _doctorRepository = doctorRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DoctorAddedOrUpdatedMessage> context)
    {
        try
        {
            _logger.LogInformation("Updating data for doctor with id {DoctorId} to maintain consistency",
                context.Message.DoctorId);
            await _doctorRepository.AddOrUpdateDoctorAsync(context.Message);
        }
        catch
        {
            _logger.LogWarning("Data update for doctor with id {DoctorId} has failed", context.Message.DoctorId);
        }
        finally
        {
            _logger.LogInformation("Data update for doctor with id {DoctorId} has finished", context.Message.DoctorId);
        }

    }
}
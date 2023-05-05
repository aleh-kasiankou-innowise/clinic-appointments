using Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Events;
using MassTransit;

namespace Innowise.Clinic.Appointments.Services.MassTransitService.Consumers;

public class PdfWithAppointmentResultsSavedConsumer : IConsumer<AppointmentResultPdfSavedEvent>
{
    private readonly IAppointmentResultsRepository _appointmentResultsRepository;

    public PdfWithAppointmentResultsSavedConsumer(IAppointmentResultsRepository appointmentResultsRepository)
    {
        _appointmentResultsRepository = appointmentResultsRepository;
    }

    public async Task Consume(ConsumeContext<AppointmentResultPdfSavedEvent> context)
    {
        var savedAppointmentResult = await
            _appointmentResultsRepository.GetAppointmentResultAsync(
                new AppointmentIdFilter().ToExpression(context.Message.AppointmentId.ToString()));
        savedAppointmentResult.PdfLink = context.Message.FileUrl;
        await _appointmentResultsRepository.UpdateAppointmentResultAsync(savedAppointmentResult);
    }
}
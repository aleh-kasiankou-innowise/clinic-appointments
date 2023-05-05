using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using Innowise.Clinic.Appointments.Services.Mappings;
using Innowise.Clinic.Appointments.Services.NotificationsService;
using Innowise.Clinic.Shared.Services.PredicateBuilder;
using DoctorFilter = Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults.DoctorFilter;

namespace Innowise.Clinic.Appointments.Services.AppointmentResultsService.Implementations;

public class AppointmentResultsService : IAppointmentResultsService
{
    private readonly IAppointmentResultsRepository _appointmentResultsRepository;
    private readonly BackgroundNotificationsService _notificationsService;

    public AppointmentResultsService(IAppointmentResultsRepository appointmentResultsRepository,
        BackgroundNotificationsService notificationsService)
    {
        _appointmentResultsRepository = appointmentResultsRepository;
        _notificationsService = notificationsService;
    }

    public async Task<ViewAppointmentResultDto> GetDoctorAppointmentResult(Guid appointmentId, Guid doctorId)
    {
        var appointmentResultIdFilterExpression = new AppointmentIdFilter().ToExpression(appointmentId.ToString());
        var appointmentResultDoctorIdExpression = new DoctorFilter().ToExpression(doctorId.ToString());
        var complexFilter = appointmentResultIdFilterExpression.And(appointmentResultDoctorIdExpression);
        var appointmentResult = await _appointmentResultsRepository.GetAppointmentResultAsync(complexFilter);
        return appointmentResult.ToFrontendPresentation();
    }

    public async Task<ViewAppointmentResultDto> GetPatientAppointmentResult(Guid appointmentId, Guid patientId)
    {
        var appointmentResultIdFilterExpression = new AppointmentIdFilter().ToExpression(appointmentId.ToString());
        var appointmentResultPatientFilterExpression = new PatientFilter().ToExpression(patientId.ToString());
        var complexFilter = appointmentResultIdFilterExpression.And(appointmentResultPatientFilterExpression);
        var appointmentResult = await _appointmentResultsRepository.GetAppointmentResultAsync(complexFilter);
        return appointmentResult.ToFrontendPresentation();
    }

    public async Task<Guid> CreateAppointmentResult(CreateAppointmentResultDto newAppointmentResult)
    {
        var appointmentResult = newAppointmentResult.ToNewAppointmentResult();
        var savedAppointmentResultId =
            await _appointmentResultsRepository.CreateAppointmentResultAsync(appointmentResult);
        await _notificationsService.EnqueueNotification(new(NotificationType.AppointmentResultNotification,
            savedAppointmentResultId));
        return savedAppointmentResultId;
    }

    public async Task UpdateAppointmentResult(Guid id, AppointmentResultEditDto updatedAppointmentResult)
    {
        var appointmentResult = updatedAppointmentResult.ToUpdatedAppointmentResult(id);
        await _appointmentResultsRepository.UpdateAppointmentResultAsync(appointmentResult);
        await _notificationsService.EnqueueNotification(new(NotificationType.AppointmentResultNotification, id));
    }
}
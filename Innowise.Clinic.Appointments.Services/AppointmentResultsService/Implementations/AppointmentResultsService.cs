using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using Innowise.Clinic.Appointments.Services.Mappings;
using Innowise.Clinic.Shared.Services.PredicateBuilder;
using MassTransit;
using DoctorFilter = Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults.DoctorFilter;
using IdFilter = Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults.IdFilter;

namespace Innowise.Clinic.Appointments.Services.AppointmentResultsService.Implementations;

public class AppointmentResultsService : IAppointmentResultsService
{
    private readonly IAppointmentResultsRepository _appointmentResultsRepository;
    private readonly IAppointmentsRepository _appointmentsRepository;
    private readonly IBus _bus;

    public AppointmentResultsService(IBus bus, IAppointmentResultsRepository appointmentResultsRepository,
        IAppointmentsRepository appointmentsRepository)
    {
        _bus = bus;
        _appointmentResultsRepository = appointmentResultsRepository;
        _appointmentsRepository = appointmentsRepository;
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

    public async Task<Guid> CreateAppointmentResult(CreateAppointmentResultDto newAppointmentResult, Guid doctorId)
    {
        var appointmentResult = newAppointmentResult.ToNewAppointmentResult();
        var appointmentResultId = await _appointmentResultsRepository.CreateAppointmentResultAsync(appointmentResult);
        
        // need patient email and full appointment info
        // todo send event to notifications service
        return appointmentResultId;
    }

    public async Task UpdateAppointmentResult(Guid id, AppointmentResultEditDto updatedAppointmentResult, Guid doctorId)
    {
        var appointmentResult = updatedAppointmentResult.ToUpdatedAppointmentResult(id);
        await _appointmentResultsRepository.UpdateAppointmentResultAsync(appointmentResult);
        // need patient email and full appointment info
        // todo send event to notification service
    }
}
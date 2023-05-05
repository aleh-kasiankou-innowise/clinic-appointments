using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using Innowise.Clinic.Shared.BaseClasses;
using Innowise.Clinic.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innowise.Clinic.Appointments.Api.Controllers;

public class AppointmentResultsController : ApiControllerBase
{
    private readonly IAppointmentResultsService _appointmentResultsService;

    public AppointmentResultsController(IAppointmentResultsService appointmentResultsService)
    {
        _appointmentResultsService = appointmentResultsService;
    }

    [HttpGet("{appointmentId:guid}")]
    [Authorize(Roles = $"{UserRoles.Doctor},{UserRoles.Patient}")]
    public async Task<ActionResult<ViewAppointmentResultDto>> GetAppointmentResult([FromRoute] Guid appointmentId)
    {
        ViewAppointmentResultDto appointment;

        if (User.IsInRole("Doctor"))
        {
            appointment = await _appointmentResultsService.GetDoctorAppointmentResult(appointmentId, GetProfileAccessId());
        }
        else
        {
            appointment = await _appointmentResultsService.GetPatientAppointmentResult(appointmentId, GetProfileAccessId());
        }

        return Ok(appointment);
    }

    [HttpPost]
    [Authorize(Roles = $"{UserRoles.Doctor}")]
    public async Task<ActionResult<Guid>> CreateAppointmentResult([FromBody] CreateAppointmentResultDto newAppointment)
    {
        return Ok((await _appointmentResultsService.CreateAppointmentResult(newAppointment))
            .ToString());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Doctor}")]
    public async Task<ActionResult> UpdateAppointmentResult([FromRoute] Guid id,
        [FromBody] AppointmentResultEditDto updatedAppointment)
    {
        await _appointmentResultsService.UpdateAppointmentResult(id, updatedAppointment);
        return Ok();
    }

    private Guid GetProfileAccessId()
    {
        return Guid.Parse(User.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.LimitedAccessToProfileClaim)?.Value ??
                          throw new InvalidOperationException());
    }
}
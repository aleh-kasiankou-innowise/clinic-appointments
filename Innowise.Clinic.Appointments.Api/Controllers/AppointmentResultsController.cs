using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using Innowise.Clinic.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innowise.Clinic.Appointments.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AppointmentResultsController : ControllerBase
{
    private readonly IAppointmentResultsService _appointmentResultsService;

    public AppointmentResultsController(IAppointmentResultsService appointmentResultsService)
    {
        _appointmentResultsService = appointmentResultsService;
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Doctor},{UserRoles.Patient}")]
    public async Task<ActionResult<ViewAppointmentResultDto>> GetAppointmentResult([FromRoute] Guid id)
    {
        ViewAppointmentResultDto appointment;

        if (User.IsInRole("Doctor"))
        {
            appointment = await _appointmentResultsService.GetDoctorAppointmentResult(id, GetProfileAccessId());
        }
        else
        {
            appointment = await _appointmentResultsService.GetPatientAppointmentResult(id, GetProfileAccessId());
        }

        return Ok(appointment);
    }

    [HttpPost]
    [Authorize(Roles = $"{UserRoles.Doctor}")]
    public async Task<ActionResult<Guid>> CreateAppointmentResult([FromBody] CreateAppointmentResultDto newAppointment)
    {
        return Ok((await _appointmentResultsService.CreateAppointmentResult(newAppointment, GetProfileAccessId()))
            .ToString());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Doctor}")]
    public async Task<ActionResult<Guid>> UpdateAppointmentResult([FromRoute] Guid id,
        [FromBody] AppointmentResultEditDto updatedAppointment)
    {
        await _appointmentResultsService.UpdateAppointmentResult(id, updatedAppointment, GetProfileAccessId());
        return Ok();
    }

    private Guid GetProfileAccessId()
    {
        return Guid.Parse(User.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.LimitedAccessToProfileClaim)?.Value ??
                          throw new InvalidOperationException());
    }
}
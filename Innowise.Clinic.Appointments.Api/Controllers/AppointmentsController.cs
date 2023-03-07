using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.RequestPipeline;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
using Innowise.Clinic.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innowise.Clinic.Appointments.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsService _appointmentsService;

    public AppointmentsController(IAppointmentsService appointmentsService)
    {
        _appointmentsService = appointmentsService;
    }

    [HttpGet("history/{patientId:guid}")]
    [Authorize(Roles = $"{UserRoles.Doctor},{UserRoles.Patient}")]
    [ProvideAccessToOwnProfileOnlyFilter($"{UserRoles.Patient}")]
    public async Task<ActionResult<IEnumerable<ViewAppointmentHistoryDto>>> GetPatientAppointmentHistory(
        [FromRoute] Guid patientId)
    {
        return Ok(await _appointmentsService.GetPatientAppointmentHistory(patientId));
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Doctor},{UserRoles.Receptionist}")]
    public async Task<ActionResult<IEnumerable<AppointmentInfoDto>>> GetListOfAppointments(
        [FromBody] AppointmentFilterBaseDto filter)
    {
        // TODO USE QUERY PARAMS INSTEAD OF BODY

        if (User.IsInRole(UserRoles.Doctor) && filter is AppointmentDoctorFilterDto doctorFilterDto)
        {
            return Ok(await _appointmentsService.GetDoctorsAppointmentsAsync(doctorFilterDto));
        }

        if (User.IsInRole(UserRoles.Receptionist) &&
            filter is AppointmentReceptionistFilterDto receptionistFilterDto)
        {
            return Ok(await _appointmentsService.GetAppointmentsAsync(receptionistFilterDto));
        }

        return BadRequest("The applied filters are incorrect.");
    }

    [HttpPost]
    [Authorize(Roles = $"{UserRoles.Patient},{UserRoles.Receptionist}")]
    public async Task<ActionResult<Guid>> CreateAppointment([FromBody] CreateAppointmentDto newAppointment)
    {
        Guid createdAppointmentId;
        if (User.IsInRole(UserRoles.Patient))
        {
            createdAppointmentId =
                await _appointmentsService.CreateAppointmentAsync(newAppointment, ExtractUserProfileId());
        }
        else
        {
            createdAppointmentId = await _appointmentsService.CreateAppointmentAsync(newAppointment);
        }

        return Ok(createdAppointmentId.ToString());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Patient},{UserRoles.Receptionist}")]
    public async Task<IActionResult> UpdateAppointment([FromRoute] Guid id,
        [FromBody] AppointmentEditTimeDto updatedAppointment)
    {
        if (User.IsInRole(UserRoles.Patient))
        {
            if (updatedAppointment.GetType() == typeof(AppointmentEditTimeDto))
            {
                await _appointmentsService.UpdateAppointmentAsync(id, updatedAppointment, ExtractUserProfileId());
                return Ok();
            }

            return Unauthorized();
        }

        if (updatedAppointment is AppointmentEditTimeAndStatusDto editTimeAndStatusDto)
        {
            await _appointmentsService.UpdateAppointmentAsync(id, editTimeAndStatusDto);
            return Ok();
        }

        throw new InvalidDtoException(
            "The appointment cannot be updated because the sent data doesn't correspond to your role. ");
    }

    private Guid ExtractUserProfileId()
    {
        var userId = User.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.LimitedAccessToProfileClaim)?.Value ??
                     throw new UserIdClaimNotFoundException();

        return Guid.Parse(userId);
    }
}
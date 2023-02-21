using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
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

    [HttpGet("history")]
    [Authorize(Roles = "Doctor,Patient")]
    public async Task<ActionResult<IEnumerable<ViewAppointmentHistoryDto>>> GetPatientAppointmentHistory(
        Guid patientId)
    // For patient ensure he has access to profile
    {

        var appointments = await _appointmentsService.GetPatientAppointmentHistory(patientId);
        // get by doctor
        // get by patient
        return Ok(appointments);
    }

    [HttpGet]
    [Authorize(Roles = "Doctor,Receptionist")]
    public async Task<ActionResult<IEnumerable<AppointmentInfoDto>>> GetListOfAppointments(
        [FromBody] AppointmentFilterBaseDto filter)
    {
        // TODO USE QUERY PARAMS INSTEAD OF BODY
        
        if (User.IsInRole("Doctor") && filter is AppointmentDoctorFilterDto doctorFilterDto)
        {
            return Ok(await _appointmentsService.GetDoctorsAppointmentsAsync(doctorFilterDto));
        }

        if (User.IsInRole("Receptionist") && filter is AppointmentReceptionistFilterDto receptionistFilterDto)
        {
            return Ok(await _appointmentsService.GetAppointmentsAsync(receptionistFilterDto));
        }

        return BadRequest("The applied filters are incorrect.");
    }

    [HttpPost]
    [Authorize(Roles = "Patient,Receptionist")]
    //TODO Patient can only create an appointment for himself
    public async Task<ActionResult<Guid>> CreateAppointment([FromBody] CreateAppointmentDto newAppointment)
    {
        return Ok((await _appointmentsService.CreateAppointmentAsync(newAppointment)).ToString());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Patient,Receptionist")]
    // TODO Patient can only edit his own appointment
    public async Task<IActionResult> UpdateAppointment([FromRoute] Guid id,
        [FromBody] AppointmentEditDto updatedAppointment)
    {
        if (User.IsInRole("Patient") && updatedAppointment is AppointmentEditStatusDto) return Unauthorized();
        await _appointmentsService.UpdateAppointmentAsync(id, updatedAppointment);
        return Ok();
    }
}
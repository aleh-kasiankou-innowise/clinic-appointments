using Innowise.Clinic.Appointments.Services.TimeSlotsService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Innowise.Clinic.Appointments.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TimeSlotsController : ControllerBase
{
    private readonly ITimeSlotsService _timeSlotsService;


    public TimeSlotsController(ITimeSlotsService timeSlotsService)
    {
        _timeSlotsService = timeSlotsService;
    }
    
    // check if day is available for service / doctor (optional)

    // send the occupied slots for  / calculate free slots?
    /*OR*/
    // Free time obj {start: DateTime, length: minutes}
}
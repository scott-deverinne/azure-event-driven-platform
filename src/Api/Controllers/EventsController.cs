using Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateEvent([FromBody] EventItem item)
    {
        return Ok(new
        {
            message = "Event received",
            eventId = item.Id,
            item.Type,
            item.Data,
            item.CreatedAt
        });
    }
}
using System.Text.Json;
using Api.Models;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IConfiguration _configuration;

    public EventsController(ServiceBusClient serviceBusClient, IConfiguration configuration)
    {
        _serviceBusClient = serviceBusClient;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventItem item)
    {
        var queueName = _configuration["ServiceBus:QueueName"];

        if (string.IsNullOrWhiteSpace(queueName))
        {
            return StatusCode(500, new { message = "Service Bus queue name is not configured." });
        }

        await using var sender = _serviceBusClient.CreateSender(queueName);

        var messageBody = JsonSerializer.Serialize(item);
        var message = new ServiceBusMessage(messageBody);

        await sender.SendMessageAsync(message);

        return Accepted(new
        {
            message = "Event queued successfully",
            eventId = item.Id
        });
    }
}
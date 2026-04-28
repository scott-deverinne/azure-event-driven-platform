using System.Text.Json;
using Api.Models;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        ServiceBusClient serviceBusClient,
        IConfiguration configuration,
        ILogger<EventsController> logger)
    {
        _serviceBusClient = serviceBusClient;
        _configuration = configuration;
        _logger = logger;
    }

    // Basic health check endpoint
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            message = "Events API is running"
        });
    }

    // Debug endpoint to verify live configuration values
    [HttpGet("config-check")]
    public IActionResult ConfigCheck()
    {
        return Ok(new
        {
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),

            // Value resolved via IConfiguration
            queueName_from_config = _configuration["ServiceBus:QueueName"],

            // Raw environment variable from Azure
            queueName_env_raw = Environment.GetEnvironmentVariable("ServiceBus__QueueName"),

            // Check Service Bus connection exists
            hasServiceBusConnection = !string.IsNullOrWhiteSpace(_configuration["ServiceBusConnection"])
        });
    }

    // Main endpoint: send event to Service Bus
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventItem item)
    {
        var queueName = _configuration["ServiceBus:QueueName"];

        // Log incoming event
        _logger.LogInformation(
            "Received API event {EventId}. Type: {Type}. Data: {Data}",
            item.Id,
            item.Type,
            item.Data);

        if (string.IsNullOrWhiteSpace(queueName))
        {
            _logger.LogError("Service Bus queue name is not configured.");
            return StatusCode(500, new { message = "Service Bus queue name is not configured." });
        }

        await using var sender = _serviceBusClient.CreateSender(queueName);

        var messageBody = JsonSerializer.Serialize(item);
        var message = new ServiceBusMessage(messageBody);

        // Correlation / tracing metadata
        message.CorrelationId = item.Id.ToString();
        message.ApplicationProperties["EventId"] = item.Id.ToString();
        message.ApplicationProperties["EventType"] = item.Type;

        // Log before sending
        _logger.LogInformation(
            "Publishing event {EventId} to Service Bus queue {QueueName}",
            item.Id,
            queueName);

        await sender.SendMessageAsync(message);

        // Log after sending
        _logger.LogInformation(
            "Published event {EventId} successfully to queue {QueueName}",
            item.Id,
            queueName);

        return Accepted(new
        {
            message = "Event queued successfully",
            eventId = item.Id
        });
    }
}
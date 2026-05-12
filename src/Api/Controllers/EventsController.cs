// CONTRACTS CHANGE: removed System.Text.Json and Api.Models because the API now publishes strongly typed financial event contracts.
using Azure.Messaging.ServiceBus;
using CloudNativePlatform.Contracts.Events;
using CloudNativePlatform.Contracts.Serialization;
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

    // CONTRACTS CHANGE:
    // Main endpoint changed from the old generic EventItem model to a strongly typed PaymentCreatedEvent.
    // This is the first typed contract endpoint. Additional financial events are added below as separate explicit endpoints.
    [HttpPost("payment-created")]
    public async Task<IActionResult> CreatePaymentCreatedEvent([FromBody] PaymentCreatedEvent financialEvent)
    {
        return await PublishFinancialEvent(financialEvent);
    }

    // CONTRACTS CHANGE:
    // Adds typed ingestion endpoint for payment settlement events.
    [HttpPost("payment-settled")]
    public async Task<IActionResult> CreatePaymentSettledEvent([FromBody] PaymentSettledEvent financialEvent)
    {
        return await PublishFinancialEvent(financialEvent);
    }

    // CONTRACTS CHANGE:
    // Adds typed ingestion endpoint for refund events.
    [HttpPost("refund-issued")]
    public async Task<IActionResult> CreateRefundIssuedEvent([FromBody] RefundIssuedEvent financialEvent)
    {
        return await PublishFinancialEvent(financialEvent);
    }

    // CONTRACTS CHANGE:
    // Adds typed ingestion endpoint for fraud-check workflow events.
    [HttpPost("fraud-check-requested")]
    public async Task<IActionResult> CreateFraudCheckRequestedEvent([FromBody] FraudCheckRequestedEvent financialEvent)
    {
        return await PublishFinancialEvent(financialEvent);
    }

    // CONTRACTS CHANGE:
    // Shared publishing flow for all FinancialEvent contract types.
    // Keeps Service Bus publishing logic in one place and standardizes metadata, correlation, and message properties.
    private async Task<IActionResult> PublishFinancialEvent(FinancialEvent financialEvent)
    {
        var queueName = _configuration["ServiceBus:QueueName"];

        if (string.IsNullOrWhiteSpace(queueName))
        {
            _logger.LogError("Service Bus queue name is not configured.");
            return StatusCode(500, new { message = "Service Bus queue name is not configured." });
        }

        // CONTRACTS CHANGE:
        // Validate required distributed-system metadata before queueing.
        if (string.IsNullOrWhiteSpace(financialEvent.EventId))
        {
            return BadRequest(new { message = "eventId is required." });
        }

        if (string.IsNullOrWhiteSpace(financialEvent.CorrelationId))
        {
            return BadRequest(new { message = "correlationId is required." });
        }

        if (string.IsNullOrWhiteSpace(financialEvent.EventType))
        {
            return BadRequest(new { message = "eventType is required." });
        }

        if (string.IsNullOrWhiteSpace(financialEvent.EventVersion))
        {
            return BadRequest(new { message = "eventVersion is required." });
        }

        if (string.IsNullOrWhiteSpace(financialEvent.Source))
        {
            return BadRequest(new { message = "source is required." });
        }

        // Log incoming event
        // CONTRACTS CHANGE:
        // Logs now use typed contract metadata instead of old EventItem Id/Type/Data fields.
        _logger.LogInformation(
            "Received financial event {EventType} with EventId {EventId} and CorrelationId {CorrelationId}",
            financialEvent.EventType,
            financialEvent.EventId,
            financialEvent.CorrelationId);

        await using var sender = _serviceBusClient.CreateSender(queueName);

        // CONTRACTS CHANGE:
        // Serialize the concrete financial event type while preserving the correct derived-event shape.
        var messageBody = FinancialEventSerializer.Serialize(financialEvent);

        var message = new ServiceBusMessage(messageBody)
        {
            // CONTRACTS CHANGE:
            // Standard Service Bus metadata now aligns with the event contract.
            MessageId = financialEvent.EventId,
            CorrelationId = financialEvent.CorrelationId,
            ContentType = "application/json",
            Subject = financialEvent.EventType
        };

        // Correlation / tracing metadata
        // CONTRACTS CHANGE:
        // ApplicationProperties now carry contract metadata used by tracing, diagnostics, and future routing.
        message.ApplicationProperties["eventId"] = financialEvent.EventId;
        message.ApplicationProperties["correlationId"] = financialEvent.CorrelationId;
        message.ApplicationProperties["eventType"] = financialEvent.EventType;
        message.ApplicationProperties["eventVersion"] = financialEvent.EventVersion;
        message.ApplicationProperties["source"] = financialEvent.Source;

        // Log before sending
        _logger.LogInformation(
            "Publishing financial event {EventType} with EventId {EventId} to Service Bus queue {QueueName}",
            financialEvent.EventType,
            financialEvent.EventId,
            queueName);

        await sender.SendMessageAsync(message);

        // Log after sending
        _logger.LogInformation(
            "Published financial event {EventType} with EventId {EventId} successfully to queue {QueueName}",
            financialEvent.EventType,
            financialEvent.EventId,
            queueName);

        return Accepted(new
        {
            message = "Financial event queued successfully",
            financialEvent.EventId,
            financialEvent.CorrelationId,
            financialEvent.EventType,
            financialEvent.EventVersion
        });
    }
}
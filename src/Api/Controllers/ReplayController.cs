using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReplayController : ControllerBase
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReplayController> _logger;

    public ReplayController(
        ServiceBusClient serviceBusClient,
        IConfiguration configuration,
        ILogger<ReplayController> logger)
    {
        _serviceBusClient = serviceBusClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Replays a dead-lettered event back into the main queue
    /// </summary>
    [HttpPost("{eventId}")]
    public async Task<IActionResult> ReplayEvent(string eventId)
    {
        var queueName = _configuration["ServiceBus:QueueName"];
        var blobConnectionString = _configuration["BlobStorageConnection"];
        var containerName = _configuration["BlobStorage:ContainerName"];

        if (string.IsNullOrWhiteSpace(queueName) ||
            string.IsNullOrWhiteSpace(blobConnectionString) ||
            string.IsNullOrWhiteSpace(containerName))
        {
            _logger.LogError("Replay configuration is invalid.");
            return StatusCode(500, new { message = "Configuration error" });
        }

        // -----------------------------
        // Locate dead-letter blob
        // -----------------------------
        var blobServiceClient = new BlobServiceClient(blobConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var blobPath = $"dead-letter/{eventId}.json";
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning("Replay failed. Dead-letter event {EventId} not found.", eventId);
            return NotFound(new { message = "Dead-letter event not found" });
        }

        // -----------------------------
        // Read event payload
        // -----------------------------
        var download = await blobClient.DownloadContentAsync();
        var json = download.Value.Content.ToString();

        _logger.LogInformation(
            "Replaying dead-letter event {EventId} from blob {BlobPath}",
            eventId,
            blobPath);

        // -----------------------------
        // Send back to Service Bus
        // -----------------------------
        await using var sender = _serviceBusClient.CreateSender(queueName);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var replayPayload = new
        {
            id = root.GetProperty("id").GetString(),
            type = "replayed-event",
            data = root.GetProperty("data").GetString(),
            createdAt = root.TryGetProperty("createdAt", out var createdAt)
                ? createdAt.GetDateTime()
                : DateTime.UtcNow
        };

        var replayJson = JsonSerializer.Serialize(replayPayload);

        var message = new ServiceBusMessage(replayJson)
        {
            CorrelationId = eventId
        };

        message.ApplicationProperties["Replayed"] = true;
        message.ApplicationProperties["EventId"] = eventId;

        await sender.SendMessageAsync(message);

        _logger.LogInformation(
            "Replayed event {EventId} to queue {QueueName}",
            eventId,
            queueName);

        return Ok(new
        {
            message = "Event replayed successfully",
            eventId = eventId
        });
    }
}
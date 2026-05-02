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

        var blobServiceClient = new BlobServiceClient(blobConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var blobPath = $"dead-letter/{eventId}.json";
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync())
        {
            return NotFound(new { message = "Dead-letter event not found" });
        }

        var download = await blobClient.DownloadContentAsync();
        var json = download.Value.Content.ToString();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var id = GetString(root, "id", "Id") ?? eventId;
        var data = GetString(root, "data", "Data") ?? "replayed from dead letter";
        var createdAt = GetDateTime(root, "createdAt", "CreatedAt") ?? DateTime.UtcNow;

        var replayPayload = new
        {
            id,
            type = "replayed-event",
            data,
            createdAt
        };

        var replayJson = JsonSerializer.Serialize(replayPayload);

        await using var sender = _serviceBusClient.CreateSender(queueName);

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
            eventId
        });
    }

    private static string? GetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value))
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static DateTime? GetDateTime(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) &&
                value.TryGetDateTime(out var dateTime))
            {
                return dateTime;
            }
        }

        return null;
    }
}
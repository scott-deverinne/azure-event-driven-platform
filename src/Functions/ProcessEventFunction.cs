using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Functions;

public class ProcessEventFunction
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public ProcessEventFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        // Inject structured logging for observability and integration with Application Insights
        _logger = loggerFactory.CreateLogger<ProcessEventFunction>();

        // Inject configuration to access Blob Storage settings
        _configuration = configuration;
    }

    [Function("ProcessEventFunction")]
    public async Task Run(
        // Service Bus trigger enables event-driven execution, decoupling API from processing
        [ServiceBusTrigger("event-queue", Connection = "ServiceBusConnection")]
        string message)
    {
        // Log raw message payload for traceability and debugging of upstream producers
        _logger.LogInformation("Received raw message: {Message}", message);

        EventItem? eventItem;

        try
        {
            // Deserialize message into strongly-typed model to enable structured processing
            eventItem = JsonSerializer.Deserialize<EventItem>(message);
        }
        catch (JsonException ex)
        {
            // Capture deserialization failures to prevent silent message loss and enable monitoring
            _logger.LogError(ex, "Failed to deserialize Service Bus message.");
            return;
        }

        if (eventItem is null)
        {
            // Guard clause to handle unexpected null payloads after deserialization
            _logger.LogWarning("Received null event after deserialization.");
            return;
        }

        if (string.IsNullOrWhiteSpace(eventItem.Type))
        {
            // Basic validation to enforce contract integrity before processing
            _logger.LogWarning("Event {EventId} is missing a type.", eventItem.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(eventItem.Data))
        {
            // Prevent downstream processing of incomplete or invalid events
            _logger.LogWarning("Event {EventId} is missing data.", eventItem.Id);
            return;
        }

        // Structured log representing the start of business processing, supports querying in Application Insights
        _logger.LogInformation(
            "Processing event {EventId}. Type: {Type}. Data: {Data}. CreatedAt: {CreatedAt}",
            eventItem.Id,
            eventItem.Type,
            eventItem.Data,
            eventItem.CreatedAt);

        // -----------------------------
        // Blob Storage Persistence
        // -----------------------------

        // Retrieve Blob Storage configuration values
        var blobConnectionString = _configuration["BlobStorageConnection"];
        var containerName = _configuration["BlobContainerName"];

        if (string.IsNullOrWhiteSpace(blobConnectionString))
        {
            // Ensure storage configuration is present before attempting persistence
            _logger.LogError("Blob storage connection string is not configured.");
            return;
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            // Ensure container name is configured
            _logger.LogError("Blob container name is not configured.");
            return;
        }

        // Create Blob service client for interacting with storage account
        var blobServiceClient = new BlobServiceClient(blobConnectionString);

        // Get reference to the container where events will be stored
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Construct a date-partitioned blob path to organise events for scalability and retrieval
        var blobPath = $"events/{eventItem.CreatedAt:yyyy/MM/dd}/{eventItem.Id}.json";

        var blobClient = blobContainerClient.GetBlobClient(blobPath);

        // Serialize event to formatted JSON for readability and downstream processing
        var json = JsonSerializer.Serialize(eventItem, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Upload event data to Blob Storage as durable storage
        await blobClient.UploadAsync(stream, overwrite: true);

        // Log successful persistence for traceability and auditability
        _logger.LogInformation(
            "Event {EventId} persisted to Blob Storage at {BlobPath}.",
            eventItem.Id,
            blobPath);

        // After validation checks and before processing log
        if (eventItem.Type == "force-fail")
        {
            _logger.LogWarning("Simulating failure for event {EventId}", eventItem.Id);
            throw new Exception("Simulated failure");
        }
        // Final processing completion log
        _logger.LogInformation("Event {EventId} processed successfully.", eventItem.Id);
    }
}
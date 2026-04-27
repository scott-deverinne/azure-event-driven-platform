using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus; // Needed for ServiceBusReceivedMessage
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
        _logger = loggerFactory.CreateLogger<ProcessEventFunction>();
        _configuration = configuration;
    }

    [Function("ProcessEventFunction")]
    public async Task Run(
        // 🔥 Changed from string → ServiceBusReceivedMessage to access metadata like CorrelationId
        [ServiceBusTrigger("%ServiceBus:QueueName%", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message)
    {
        try
        {
            // Extract raw message body
            var messageBody = message.Body.ToString();

            // 🔥 Log correlation ID for distributed tracing
            _logger.LogInformation(
                "Received message with CorrelationId: {CorrelationId}",
                message.CorrelationId);

            _logger.LogInformation("Received raw message: {Message}", messageBody);

            EventItem? eventItem;

            try
            {
                eventItem = JsonSerializer.Deserialize<EventItem>(messageBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Service Bus message.");
                return;
            }

            if (eventItem is null)
            {
                _logger.LogWarning("Received null event after deserialization.");
                return;
            }

            if (string.IsNullOrWhiteSpace(eventItem.Type))
            {
                _logger.LogWarning("Event {EventId} is missing a type.", eventItem.Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(eventItem.Data))
            {
                _logger.LogWarning("Event {EventId} is missing data.", eventItem.Id);
                return;
            }

            // 🔥 Main processing log (key for observability queries)
            _logger.LogInformation(
                "Processing event {EventId}. Type: {Type}. Data: {Data}. CreatedAt: {CreatedAt}",
                eventItem.Id,
                eventItem.Type,
                eventItem.Data,
                eventItem.CreatedAt);

            var queueName = _configuration["ServiceBus:QueueName"];
            var blobConnectionString = _configuration["BlobStorageConnection"];
            var containerName = _configuration["BlobStorage:ContainerName"];

            _logger.LogInformation(
                "Resolved config. Queue: {QueueName}. Blob connection set: {HasConnection}. Container: {ContainerName}",
                queueName,
                !string.IsNullOrWhiteSpace(blobConnectionString),
                containerName);

            if (string.IsNullOrWhiteSpace(queueName))
            {
                _logger.LogError("Service Bus queue name is not configured.");
                return;
            }

            if (string.IsNullOrWhiteSpace(blobConnectionString))
            {
                _logger.LogError("Blob storage connection string is not configured.");
                return;
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                _logger.LogError("Blob container name is not configured.");
                return;
            }

            // Create Blob client
            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await blobContainerClient.CreateIfNotExistsAsync();

            var blobPath = $"events/{eventItem.CreatedAt:yyyy/MM/dd}/{eventItem.Id}.json";
            var blobClient = blobContainerClient.GetBlobClient(blobPath);

            var json = JsonSerializer.Serialize(eventItem, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream, overwrite: true);

            // 🔥 Persistence log (important for debugging pipeline completion)
            _logger.LogInformation(
                "Event {EventId} persisted to Blob Storage at {BlobPath}.",
                eventItem.Id,
                blobPath);

            // Simulate failure scenario for testing retries / DLQ
            if (eventItem.Type == "force-fail")
            {
                _logger.LogWarning("Simulating failure for event {EventId}", eventItem.Id);
                throw new Exception("Simulated failure");
            }

            _logger.LogInformation("Event {EventId} processed successfully.", eventItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled failure in ProcessEventFunction.");
            throw;
        }
    }
}
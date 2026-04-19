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
        _logger = loggerFactory.CreateLogger<ProcessEventFunction>();
        _configuration = configuration;
    }

    [Function("ProcessEventFunction")]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBus:QueueName%", Connection = "ServiceBusConnection")]
        string message)
    {
        try
        {
            _logger.LogInformation("Received raw message: {Message}", message);

            EventItem? eventItem;

            try
            {
                eventItem = JsonSerializer.Deserialize<EventItem>(message);
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

            _logger.LogInformation(
                "Processing event {EventId}. Type: {Type}. Data: {Data}. CreatedAt: {CreatedAt}",
                eventItem.Id,
                eventItem.Type,
                eventItem.Data,
                eventItem.CreatedAt);

            var blobConnectionString = _configuration["BlobStorageConnection"];
            var containerName = _configuration["BlobStorage:ContainerName"];

            _logger.LogInformation(
                "Blob config present. Connection set: {HasConnection}. Container: {ContainerName}",
                !string.IsNullOrWhiteSpace(blobConnectionString),
                containerName);

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

            _logger.LogInformation(
                "Event {EventId} persisted to Blob Storage at {BlobPath}.",
                eventItem.Id,
                blobPath);

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
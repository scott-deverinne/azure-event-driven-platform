using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Functions;

public class ProcessDeadLetterFunction
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public ProcessDeadLetterFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<ProcessDeadLetterFunction>();
        _configuration = configuration;
    }

    [Function("ProcessDeadLetterFunction")]
    public async Task Run(
        // Listen to the dead-letter queue for the configured Service Bus queue
        [ServiceBusTrigger("%ServiceBus:QueueName%/$DeadLetterQueue", Connection = "ServiceBusConnection")]
        string message)
    {
        try
        {
            _logger.LogError("Received dead-letter message: {Message}", message);

            EventItem? eventItem;

            try
            {
                eventItem = JsonSerializer.Deserialize<EventItem>(message);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize dead-letter message.");

                // Store invalid DLQ payload using a generated file name
                await StoreDeadLetterPayloadAsync(
                    message,
                    $"dead-letter/invalid/{Guid.NewGuid()}.json");

                return;
            }

            if (eventItem is null)
            {
                _logger.LogWarning("Dead-letter message deserialized to null.");

                await StoreDeadLetterPayloadAsync(
                    message,
                    $"dead-letter/null/{Guid.NewGuid()}.json");

                return;
            }

            // Store failed event for audit and future replay
            var deadLetterPath = $"dead-letter/{eventItem.Id}.json";

            await StoreDeadLetterPayloadAsync(message, deadLetterPath);

            _logger.LogInformation(
                "Stored dead-letter event {EventId} at {DeadLetterPath}",
                eventItem.Id,
                deadLetterPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled failure in ProcessDeadLetterFunction.");
            throw;
        }
    }

    private async Task StoreDeadLetterPayloadAsync(string message, string blobPath)
    {
        var blobConnectionString = _configuration["BlobStorageConnection"];
        var containerName = _configuration["BlobStorage:ContainerName"];

        if (string.IsNullOrWhiteSpace(blobConnectionString))
        {
            throw new InvalidOperationException("Blob storage connection string is not configured.");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException("Blob container name is not configured.");
        }

        var blobServiceClient = new BlobServiceClient(blobConnectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await blobContainerClient.CreateIfNotExistsAsync();

        var blobClient = blobContainerClient.GetBlobClient(blobPath);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(message));
        await blobClient.UploadAsync(stream, overwrite: true);
    }
}
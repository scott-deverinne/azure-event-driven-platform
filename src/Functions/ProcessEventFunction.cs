using System.Text.Json;
using Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Functions;

public class ProcessEventFunction
{
    private readonly ILogger _logger;

    public ProcessEventFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ProcessEventFunction>();
    }

    [Function("ProcessEventFunction")]
    public void Run(
        [ServiceBusTrigger("event-queue", Connection = "ServiceBusConnection")]
        string message)
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

        // Simulated business processing
        _logger.LogInformation("Event {EventId} processed successfully.", eventItem.Id);
    }
}
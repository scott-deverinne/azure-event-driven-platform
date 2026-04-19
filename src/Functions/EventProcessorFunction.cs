using System.Text.Json;
using Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Functions;

public class EventProcessorFunction
{
    private readonly ILogger _logger;

    public EventProcessorFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EventProcessorFunction>();
    }

    [Function("EventProcessorFunction")]
    public void Run(
        // Uses a separate queue so this baseline processor does not compete
        // with ProcessEventFunction for messages from the main event queue
        [ServiceBusTrigger("event-queue-test", Connection = "ServiceBusConnection")]
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
            _logger.LogError(ex, "Failed to deserialize message.");
            return;
        }

        if (eventItem is null)
        {
            _logger.LogWarning("Event deserialized to null.");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventId}. Type: {Type}. Data: {Data}. CreatedAt: {CreatedAt}",
            eventItem.Id,
            eventItem.Type,
            eventItem.Data,
            eventItem.CreatedAt
        );

        // Simulated processing step
        _logger.LogInformation("Event {EventId} processed successfully.", eventItem.Id);
    }
}
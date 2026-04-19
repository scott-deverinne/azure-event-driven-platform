using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Api.Models; // make sure this matches your shared model

namespace Functions;

public class EventProcessorFunction
{
    private readonly ILogger<EventProcessorFunction> _logger;

    public EventProcessorFunction(ILogger<EventProcessorFunction> logger)
    {
        _logger = logger;
    }

    [Function("EventProcessor")]
    public void Run(
        [ServiceBusTrigger("%ServiceBus:QueueName%", Connection = "ServiceBusConnection")]
        string message)
    {
        var eventItem = JsonSerializer.Deserialize<EventItem>(message);

        _logger.LogInformation(
            "Processed event {EventId} | Type: {Type} | Data: {Data}",
            eventItem?.Id,
            eventItem?.Type,
            eventItem?.Data
        );
    }
}
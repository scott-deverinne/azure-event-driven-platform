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
        _logger.LogInformation("Received message: {Message}", message);
    }
}
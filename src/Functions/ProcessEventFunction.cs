using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
// CONTRACTS CHANGE: removed Functions.Models because processing now uses the shared typed event contracts.
using CloudNativePlatform.Contracts.Events;
using CloudNativePlatform.Contracts.Serialization;
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
        // Use string binding for reliable Service Bus trigger indexing
        [ServiceBusTrigger("%ServiceBus:QueueName%", Connection = "ServiceBusConnection")]
        string message,
        FunctionContext context)
    {
        try
        {
            // Log retry attempt details when Azure Functions retries this execution
            if (context.RetryContext is not null)
            {
                _logger.LogWarning(
                    "Retry attempt {RetryCount} of {MaxRetryCount}.",
                    context.RetryContext.RetryCount,
                    context.RetryContext.MaxRetryCount);
            }

            // Extract raw message body
            var messageBody = message;

            _logger.LogInformation("Received raw message: {Message}", messageBody);

            _logger.LogWarning(
                "FUNCTION TRIGGERED - QueueName: {QueueName}",
                _configuration["ServiceBus:QueueName"]);

            // CONTRACTS CHANGE:
            // Deserialize into the shared FinancialEvent abstraction instead of the old local EventItem model.
            FinancialEvent financialEvent;

            try
            {
                financialEvent = FinancialEventSerializer.Deserialize(messageBody);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException)
            {
                _logger.LogError(ex, "Failed to deserialize typed financial event message.");
                throw;
            }

            // -----------------------------
            // Validation
            // -----------------------------
            // CONTRACTS CHANGE:
            // Validation now checks common financial event metadata instead of old EventItem Type/Data fields.
            if (financialEvent is null)
            {
                _logger.LogWarning("Received null financial event after deserialization.");
                return;
            }

            if (string.IsNullOrWhiteSpace(financialEvent.EventId))
            {
                _logger.LogWarning("Financial event is missing eventId.");
                return;
            }

            if (string.IsNullOrWhiteSpace(financialEvent.EventType))
            {
                _logger.LogWarning("Financial event {EventId} is missing eventType.", financialEvent.EventId);
                return;
            }

            if (string.IsNullOrWhiteSpace(financialEvent.CorrelationId))
            {
                _logger.LogWarning("Financial event {EventId} is missing correlationId.", financialEvent.EventId);
                return;
            }

            if (string.IsNullOrWhiteSpace(financialEvent.EventVersion))
            {
                _logger.LogWarning("Financial event {EventId} is missing eventVersion.", financialEvent.EventId);
                return;
            }

            if (string.IsNullOrWhiteSpace(financialEvent.Source))
            {
                _logger.LogWarning("Financial event {EventId} is missing source.", financialEvent.EventId);
                return;
            }

            _logger.LogInformation(
                "Processing financial event {EventType}. EventId: {EventId}. CorrelationId: {CorrelationId}. OccurredAtUtc: {OccurredAtUtc}",
                financialEvent.EventType,
                financialEvent.EventId,
                financialEvent.CorrelationId,
                financialEvent.OccurredAtUtc);

            // -----------------------------
            // Controlled failure simulation
            // -----------------------------
            // This intentionally fails before any side effects.
            // No event blob or idempotency marker should be written for this test case.
            // CONTRACTS CHANGE:
            // Failure simulation now uses the typed eventType field.
            if (financialEvent.EventType == "force-fail")
            {
                _logger.LogWarning("Simulating failure for event {EventId}", financialEvent.EventId);
                throw new Exception("Simulated failure");
            }

            // -----------------------------
            // Configuration
            // -----------------------------
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

            // -----------------------------
            // Blob setup
            // -----------------------------
            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await blobContainerClient.CreateIfNotExistsAsync();

            // -----------------------------
            // Idempotency check
            // -----------------------------
            // Check if this event has already been processed
            // CONTRACTS CHANGE:
            // Idempotency marker now uses financialEvent.EventId.
            var processedPath = $"processed-events/{financialEvent.EventId}.json";
            var processedBlob = blobContainerClient.GetBlobClient(processedPath);

            if (await processedBlob.ExistsAsync())
            {
                _logger.LogWarning(
                    "Duplicate financial event detected. Event {EventId} has already been processed. Skipping.",
                    financialEvent.EventId);

                return;
            }

            // CONTRACTS CHANGE:
            // Explicit typed event handling hook. This is where future business workflows branch safely by contract type.
            await ProcessTypedEvent(financialEvent);

            // -----------------------------
            // Main processing: persist event
            // -----------------------------
            // CONTRACTS CHANGE:
            // Blob path now uses occurredAtUtc and eventId from the financial event contract.
            var blobPath = $"events/{financialEvent.OccurredAtUtc:yyyy/MM/dd}/{financialEvent.EventId}.json";
            var blobClient = blobContainerClient.GetBlobClient(blobPath);

            // CONTRACTS CHANGE:
            // Persist the typed event through the shared serializer.
            var json = FinancialEventSerializer.Serialize(financialEvent);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream, overwrite: true);

            _logger.LogInformation(
                "Financial event {EventId} persisted to Blob Storage at {BlobPath}.",
                financialEvent.EventId,
                blobPath);

            // -----------------------------
            // Write idempotency marker
            // -----------------------------
            // Marker is written only after successful processing
            // CONTRACTS CHANGE:
            // Marker now includes correlation and contract metadata for traceability.
            var markerContent = JsonSerializer.Serialize(new
            {
                eventId = financialEvent.EventId,
                correlationId = financialEvent.CorrelationId,
                eventType = financialEvent.EventType,
                eventVersion = financialEvent.EventVersion,
                processedAtUtc = DateTime.UtcNow
            });

            using var markerStream = new MemoryStream(Encoding.UTF8.GetBytes(markerContent));
            await processedBlob.UploadAsync(markerStream, overwrite: true);

            _logger.LogInformation(
                "Idempotency marker written for financial event {EventId}",
                financialEvent.EventId);

            _logger.LogInformation(
                "Financial event {EventType} with EventId {EventId} processed successfully.",
                financialEvent.EventType,
                financialEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled failure in ProcessEventFunction.");
            throw;
        }
    }

    // CONTRACTS CHANGE:
    // New typed event dispatch method.
    // This keeps contract-specific logic explicit and makes future event handlers easy to extend.
    private Task ProcessTypedEvent(FinancialEvent financialEvent)
    {
        switch (financialEvent)
        {
            case PaymentCreatedEvent paymentCreated:
                _logger.LogInformation(
                    "Handled PaymentCreatedEvent. PaymentId: {PaymentId}. Amount: {Amount} {Currency}",
                    paymentCreated.PaymentId,
                    paymentCreated.Amount,
                    paymentCreated.Currency);
                break;

            case PaymentSettledEvent paymentSettled:
                _logger.LogInformation(
                    "Handled PaymentSettledEvent. PaymentId: {PaymentId}. SettlementId: {SettlementId}",
                    paymentSettled.PaymentId,
                    paymentSettled.SettlementId);
                break;

            case RefundIssuedEvent refundIssued:
                _logger.LogInformation(
                    "Handled RefundIssuedEvent. RefundId: {RefundId}. PaymentId: {PaymentId}",
                    refundIssued.RefundId,
                    refundIssued.PaymentId);
                break;

            case FraudCheckRequestedEvent fraudCheckRequested:
                _logger.LogInformation(
                    "Handled FraudCheckRequestedEvent. PaymentId: {PaymentId}. RiskLevel: {RiskLevel}",
                    fraudCheckRequested.PaymentId,
                    fraudCheckRequested.RiskLevel);
                break;

            default:
                throw new NotSupportedException(
                    $"Unsupported financial event type: {financialEvent.EventType}");
        }

        return Task.CompletedTask;
    }
}
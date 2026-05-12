using System.Text.Json;
using CloudNativePlatform.Contracts.Events;

namespace CloudNativePlatform.Contracts.Serialization;

public static class FinancialEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static FinancialEvent Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("eventType", out var eventTypeElement))
        {
            throw new InvalidOperationException("Missing required property: eventType.");
        }

        var eventType = eventTypeElement.GetString();

        return eventType switch
        {
            FinancialEventTypes.PaymentCreated =>
                JsonSerializer.Deserialize<PaymentCreatedEvent>(json, Options)!,

            FinancialEventTypes.PaymentSettled =>
                JsonSerializer.Deserialize<PaymentSettledEvent>(json, Options)!,

            FinancialEventTypes.RefundIssued =>
                JsonSerializer.Deserialize<RefundIssuedEvent>(json, Options)!,

            FinancialEventTypes.FraudCheckRequested =>
                JsonSerializer.Deserialize<FraudCheckRequestedEvent>(json, Options)!,

            _ => throw new NotSupportedException($"Unsupported eventType: {eventType}")
        };
    }

    public static string Serialize(FinancialEvent financialEvent)
    {
        return JsonSerializer.Serialize(financialEvent, financialEvent.GetType(), Options);
    }
}

namespace CloudNativePlatform.Contracts.Events;

public sealed record PaymentCreatedEvent : FinancialEvent
{
    public required string PaymentId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PaymentMethod { get; init; }
}

namespace CloudNativePlatform.Contracts.Events;

public sealed record RefundIssuedEvent : FinancialEvent
{
    public required string RefundId { get; init; }
    public required string PaymentId { get; init; }
    public required decimal RefundAmount { get; init; }
    public required string Currency { get; init; }
    public required string Reason { get; init; }
}

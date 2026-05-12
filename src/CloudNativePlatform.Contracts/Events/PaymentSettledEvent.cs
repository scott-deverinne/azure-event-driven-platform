namespace CloudNativePlatform.Contracts.Events;

public sealed record PaymentSettledEvent : FinancialEvent
{
    public required string PaymentId { get; init; }
    public required string SettlementId { get; init; }
    public required decimal SettledAmount { get; init; }
    public required string Currency { get; init; }
    public required DateTime SettledAtUtc { get; init; }
}

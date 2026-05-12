namespace CloudNativePlatform.Contracts.Events;

public abstract record FinancialEvent
{
    public required string EventId { get; init; }
    public required string CorrelationId { get; init; }
    public required string EventType { get; init; }
    public required string EventVersion { get; init; }
    public required DateTime OccurredAtUtc { get; init; }
    public required string Source { get; init; }
}

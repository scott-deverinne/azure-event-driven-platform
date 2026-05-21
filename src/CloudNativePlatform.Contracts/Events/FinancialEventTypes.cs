namespace CloudNativePlatform.Contracts.Events;

public static class FinancialEventTypes
{
    public const string PaymentCreated = "payment.created";
    public const string PaymentSettled = "payment.settled";
    public const string RefundIssued = "refund.issued";
    public const string FraudCheckRequested = "fraud-check.requested";
}

using DBPE.Messaging.Abstractions;
using DBPE.Messaging.Attributes;

namespace DBPE.Example.Contracts;

[QueueName("payments")]
[ContractConfig(ExchangeName = "payments", Persistent = true)]
public class PaymentContract : IContract
{
    public string PaymentId { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string TransactionId { get; set; } = string.Empty;
    public PaymentDetails? Details { get; set; }
}

public class PaymentDetails
{
    public string? CardLast4 { get; set; }
    public string? CardType { get; set; }
    public string? BillingAddress { get; set; }
    public string? AuthorizationCode { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded
}
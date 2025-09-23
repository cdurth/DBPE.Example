using DBPE.Messaging.Abstractions;

namespace DBPE.Example.Contracts;

public class PaymentProcessedContract : IContract
{
    public string PaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
using DBPE.Messaging.Abstractions;
using DBPE.Messaging.Attributes;

namespace DBPE.Example.Contracts;

[QueueName("order-events")]
public class OrderProcessedEvent : IContract
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string ProcessedBy { get; set; } = string.Empty;
}
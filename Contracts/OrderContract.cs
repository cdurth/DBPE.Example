using DBPE.Messaging.Abstractions;
using DBPE.Messaging.Attributes;

namespace DBPE.Example.Contracts;

[QueueName("orders")]
[ContractConfig(ExchangeName = "orders", Persistent = true)]
public class OrderContract : IContract
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
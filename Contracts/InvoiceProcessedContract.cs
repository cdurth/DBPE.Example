using DBPE.Messaging.Abstractions;
using DBPE.Messaging.Attributes;

namespace DBPE.Example.Contracts;

[ContractConfig(RoutingKey = "invoice.processed")]
public class InvoiceProcessedContract : IContract
{
    public string InvoiceId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public List<InvoiceItemContract> Items { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public string SourceFile { get; set; } = string.Empty;
}

public class InvoiceItemContract
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
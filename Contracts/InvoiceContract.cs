using DBPE.Messaging.Abstractions;
using DBPE.Messaging.Attributes;

namespace DBPE.Example.Contracts;

[QueueName("invoices")]
[ContractConfig(ExchangeName = "invoices", Persistent = true)]
public class InvoiceContract : IContract
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public List<InvoiceLineItem> LineItems { get; set; } = [];
    public BillingAddress? BillingAddress { get; set; }
    public PaymentTerms? PaymentTerms { get; set; }
}

public class InvoiceLineItem
{
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class BillingAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class PaymentTerms
{
    public int DaysToPayment { get; set; } = 30;
    public decimal? DiscountPercentage { get; set; }
    public int? EarlyPaymentDays { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public enum InvoiceStatus
{
    Draft,
    Sent,
    Viewed,
    Paid,
    Overdue,
    Cancelled
}
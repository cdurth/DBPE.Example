using DBPE.Example.Contracts;
using DBPE.Messaging.Abstractions;
using DBPE.Messaging.Attributes;
using Serilog;

namespace DBPE.Example.Consumers;

[Concurrency(1)] // Process invoices sequentially
[Retry(maxAttempts: 5, delaySeconds: 2, exponentialBackoff: true)] // Retry up to 5 times with exponential backoff (2s, 4s, 8s, 16s)
public class InvoiceProcessedConsumer : IConsumer<InvoiceProcessedContract>
{
    private readonly ILogger _logger;

    public InvoiceProcessedConsumer(ILogger logger)
    {
        _logger = logger;
    }

    public async Task Handle(InvoiceProcessedContract message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Information("Processing Invoice Message");
            _logger.Information("   Invoice ID: {InvoiceId}", message.InvoiceId);
            _logger.Information("   Customer: {CustomerName}", message.CustomerName);
            _logger.Information("   Amount: ${Amount:F2}", message.Amount);
            _logger.Information("   Invoice Date: {InvoiceDate:yyyy-MM-dd}", message.InvoiceDate);
            _logger.Information("   Source File: {SourceFile}", message.SourceFile);
            _logger.Information("   Items Count: {ItemCount}", message.Items.Count);
            
            if (message.Items.Any())
            {
                _logger.Information("   Invoice Items:");
                foreach (var item in message.Items)
                {
                    _logger.Information("      - {Description} - Qty: {Quantity} @ ${UnitPrice:F2} = ${Total:F2}", 
                        item.Description, item.Quantity, item.UnitPrice, item.Total);
                }
            }

            _logger.Information("   Processed At: {ProcessedAt:yyyy-MM-dd HH:mm:ss}", message.ProcessedAt);

            // Simulate some processing work
            await Task.Delay(500);

            // TEST ERROR HANDLING: Throw an exception for testing purposes
            // Remove this section for production use
            if (message.InvoiceId.Contains("TEST-ERROR") || message.Amount > 1000)
            {
                throw new InvalidOperationException($"TEST: Simulating invoice processing failure for {message.InvoiceId} with amount ${message.Amount:F2}");
            }

            _logger.Information("Invoice {InvoiceId} processing completed successfully", message.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing invoice {InvoiceId}", message.InvoiceId);
            throw;
        }
    }
}
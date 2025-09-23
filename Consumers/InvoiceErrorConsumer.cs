using DBPE.Example.Contracts;
using DBPE.Messaging.Abstractions;
using Serilog;

namespace DBPE.Example.Consumers;

/// <summary>
/// Error consumer that handles failed InvoiceProcessedContract messages
/// Demonstrates comprehensive error handling for invoice processing failures
/// </summary>
public class InvoiceErrorConsumer : IErrorConsumer<InvoiceProcessedContract>
{
    private readonly ILogger _logger;

    public InvoiceErrorConsumer(ILogger logger)
    {
        _logger = logger;
    }

    public async Task Handle(ErrorMessage<InvoiceProcessedContract> errorMessage)
    {
        var invoice = errorMessage.OriginalMessage;
        
        _logger.Error("🚨 InvoiceErrorConsumer: Handling failed invoice processing for {InvoiceId}", invoice.InvoiceId);
        _logger.Error("   Customer: {CustomerName}", invoice.CustomerName);
        _logger.Error("   Amount: ${Amount:F2}", invoice.Amount);
        _logger.Error("   Source File: {SourceFile}", invoice.SourceFile);
        _logger.Error("   Error Type: {ErrorType}", errorMessage.ErrorType);
        _logger.Error("   Error Message: {ErrorMessage}", errorMessage.Message);
        _logger.Error("   Retry Count: {RetryCount}", errorMessage.RetryCount);
        _logger.Error("   Consumer: {ConsumerType}", errorMessage.ConsumerType);
        _logger.Error("   Queue: {QueueName}", errorMessage.QueueName);
        _logger.Error("   Occurred At: {OccurredAt}", errorMessage.OccurredAt);
        
        if (!string.IsNullOrEmpty(errorMessage.StackTrace))
        {
            _logger.Error("   Stack Trace: {StackTrace}", errorMessage.StackTrace);
        }

        throw new Exception("Simulated error for testing purposes");

        try
        {
            // Perform error-specific processing based on error type
            await ProcessInvoiceError(errorMessage);
            
            _logger.Information("✅ InvoiceErrorConsumer: Error handled successfully for invoice {InvoiceId}", invoice.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ InvoiceErrorConsumer: Failed to handle error for invoice {InvoiceId}", invoice.InvoiceId);
            throw;
        }
    }

    private async Task ProcessInvoiceError(ErrorMessage<InvoiceProcessedContract> errorMessage)
    {
        var invoice = errorMessage.OriginalMessage;
        var errorType = errorMessage.ErrorType;

        // Simulate processing time
        await Task.Delay(100);

        if (errorType.Contains("ValidationException"))
        {
            await HandleInvoiceValidationError(invoice, errorMessage);
        }
        else if (errorType.Contains("TimeoutException"))
        {
            await HandleInvoiceTimeoutError(invoice, errorMessage);
        }
        else if (errorType.Contains("InvalidOperationException"))
        {
            await HandleInvoiceOperationError(invoice, errorMessage);
        }
        else
        {
            await HandleGenericInvoiceError(invoice, errorMessage);
        }
    }

    private async Task HandleInvoiceValidationError(InvoiceProcessedContract invoice, ErrorMessage<InvoiceProcessedContract> errorMessage)
    {
        _logger.Warning("📋 InvoiceErrorConsumer: Handling validation error for invoice {InvoiceId}", invoice.InvoiceId);
        
        // Simulate validation error handling
        await Task.Delay(50);
        
        // In a real scenario, you might:
        // - Send the invoice back to the source system for correction
        // - Create a manual review task
        // - Notify accounting team
        
        _logger.Information("📋 Validation error handled - Invoice {InvoiceId} marked for manual review", invoice.InvoiceId);
    }

    private async Task HandleInvoiceTimeoutError(InvoiceProcessedContract invoice, ErrorMessage<InvoiceProcessedContract> errorMessage)
    {
        _logger.Warning("⏰ InvoiceErrorConsumer: Handling timeout error for invoice {InvoiceId}", invoice.InvoiceId);
        
        await Task.Delay(30);
        
        // In a real scenario, you might:
        // - Check if external services are available
        // - Queue for retry during off-peak hours
        // - Process with reduced functionality
        
        _logger.Information("⏰ Timeout error handled - Invoice {InvoiceId} queued for retry", invoice.InvoiceId);
    }

    private async Task HandleInvoiceOperationError(InvoiceProcessedContract invoice, ErrorMessage<InvoiceProcessedContract> errorMessage)
    {
        _logger.Warning("⚙️ InvoiceErrorConsumer: Handling operation error for invoice {InvoiceId}", invoice.InvoiceId);
        
        await Task.Delay(40);
        
        // In a real scenario, you might:
        // - Apply business logic corrections
        // - Use alternative processing path
        // - Generate error report
        
        _logger.Information("⚙️ Operation error handled - Invoice {InvoiceId} processed with corrections", invoice.InvoiceId);
    }

    private async Task HandleGenericInvoiceError(InvoiceProcessedContract invoice, ErrorMessage<InvoiceProcessedContract> errorMessage)
    {
        _logger.Warning("❓ InvoiceErrorConsumer: Handling generic error for invoice {InvoiceId}", invoice.InvoiceId);
        
        await Task.Delay(25);
        
        // In a real scenario, you might:
        // - Log to external monitoring system
        // - Create support ticket
        // - Send notification to admin
        // - Store in DLQ for manual investigation
        
        _logger.Information("❓ Generic error handled - Invoice {InvoiceId} logged and stored for investigation", invoice.InvoiceId);
    }
}
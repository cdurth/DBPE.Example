using DBPE.Example.Contracts;
using DBPE.Messaging.Abstractions;
using DBPE.Messaging.Attributes;
using DBPE.Messaging.Configuration;
using DBPE.WebHooks.Correlation;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DBPE.Example.Consumers;

/// <summary>
/// PaymentConsumer demonstrates Advanced Error Handling mode
/// - Errors are stored in SQLite database
/// - Automatic forwarding to PaymentErrorConsumer
/// - Full audit trail and error tracking
/// </summary>
[Concurrency(5)] // Allow up to 5 concurrent payment processing
[QueueName("payments")]
[ConsumerConfig(PrefetchCount = 5, ConcurrentLimit = 3, Durable = true)]
[ErrorHandling(Mode = ErrorHandlingMode.Advanced)] // Uses database + error consumer forwarding
public class PaymentConsumer : IConsumer<PaymentProcessedContract>
{
    private readonly IDbpeMessageBus _messageBus;
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Random _random = new();

    public PaymentConsumer(IDbpeMessageBus messageBus, ILogger logger, IServiceProvider serviceProvider)
    {
        _messageBus = messageBus;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Handle(PaymentProcessedContract message, CancellationToken cancellationToken = default)
    {
        _logger.Information("🔔 Processing Payment {PaymentId} with amount ${Amount:F2} {Currency}", 
            message.PaymentId, (decimal)message.Amount / 100, message.Currency?.ToUpper());

        // Check if we have a correlation ID to track
        Guid? correlationId = null;
        
        // Debug: Log metadata contents
        if (message.Metadata != null)
        {
            _logger.Information("📋 Metadata contains {Count} items", message.Metadata.Count);
            foreach (var kvp in message.Metadata)
            {
                _logger.Information("  - {Key}: {Value}", kvp.Key, kvp.Value);
            }
            
            if (message.Metadata.TryGetValue("CorrelationId", out var correlationIdStr))
            {
                _logger.Information("Found CorrelationId in metadata: {Value}", correlationIdStr);
                if (Guid.TryParse(correlationIdStr?.ToString(), out var parsedId))
                {
                    correlationId = parsedId;
                    _logger.Information("📌 Tracking correlation ID: {CorrelationId}", correlationId);
                }
                else
                {
                    _logger.Warning("Failed to parse correlation ID: {Value}", correlationIdStr);
                }
            }
            else
            {
                _logger.Warning("CorrelationId not found in metadata");
            }
        }
        else
        {
            _logger.Warning("Metadata is null");
        }

        try
        {
            // Simulate various payment processing scenarios that can fail
            await ProcessPayment(message, cancellationToken);

            _logger.Information("✅ Payment {PaymentId} processed successfully", message.PaymentId);

            // Mark correlation as completed if we have one
            if (correlationId.HasValue)
            {
                await MarkCorrelationCompleted(correlationId.Value, message.PaymentId, "Payment processed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Failed to process payment {PaymentId}", message.PaymentId);
            
            // Mark correlation as failed if we have one
            if (correlationId.HasValue)
            {
                await MarkCorrelationFailed(correlationId.Value, message.PaymentId, ex.Message);
            }
            
            throw; // Will trigger Advanced error handling (database storage + error consumer)
        }
    }

    private async Task MarkCorrelationCompleted(Guid correlationId, string paymentId, string message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var correlationService = scope.ServiceProvider.GetService<IWebHookCorrelationService>();
            var correlationStore = scope.ServiceProvider.GetService<IWebHookCorrelationStore>();
            
            // Verify correlation exists before completing it
            if (correlationStore != null)
            {
                var existingCorrelation = await correlationStore.GetCorrelation(correlationId);
                if (existingCorrelation == null)
                {
                    _logger.Warning("Correlation {CorrelationId} not found in store", correlationId);
                    return;
                }
            }
            
            if (correlationService != null)
            {
                await correlationService.CompleteCorrelation(correlationId, new ProcessingResult
                {
                    Success = true,
                    Status = "completed",
                    ErrorMessage = null,
                    Data = new { paymentId, status = "completed", timestamp = DateTime.UtcNow, message }
                });
                
                _logger.Information("Marked correlation {CorrelationId} as completed", correlationId);
            }
            else
            {
                _logger.Debug("Correlation service not available");
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to mark correlation as completed");
        }
    }

    private async Task MarkCorrelationFailed(Guid correlationId, string paymentId, string errorMessage)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var correlationService = scope.ServiceProvider.GetService<IWebHookCorrelationService>();
            
            if (correlationService != null)
            {
                await correlationService.CompleteCorrelation(correlationId, new ProcessingResult
                {
                    Success = false,
                    Status = "failed",
                    ErrorMessage = errorMessage,
                    Data = new { paymentId, status = "failed", timestamp = DateTime.UtcNow }
                });
                
                _logger.Warning("❌ Marked correlation {CorrelationId} as failed", correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to mark correlation as failed");
        }
    }

    private async Task ProcessPayment(PaymentProcessedContract payment, CancellationToken cancellationToken)
    {
        // Simulate processing time
        await Task.Delay(200, cancellationToken);

        // Simulate different types of failures for demonstration
        var scenario = _random.Next(1, 100);

        switch (scenario)
        {
            case <= 10: // 10% chance of payment validation failure
                throw new InvalidOperationException($"Payment validation failed: Invalid card number for payment {payment.PaymentId}");
                
            case <= 15: // 5% chance of insufficient funds
                throw new PaymentDeclinedException($"Payment {payment.PaymentId} declined: Insufficient funds");
                
            case <= 20: // 5% chance of fraud detection
                throw new FraudException($"Payment {payment.PaymentId} flagged by fraud detection system");
                
            case <= 25: // 5% chance of timeout
                throw new TimeoutException($"Payment processing timeout for {payment.PaymentId}");
                
            case <= 30: // 5% chance of external service failure
                throw new ExternalServiceException($"Payment gateway unavailable for {payment.PaymentId}");
        }

        // Successful processing simulation
        _logger.Information("💳 Payment Details:");
        _logger.Information("   - Payment ID: {PaymentId}", payment.PaymentId);
        _logger.Information("   - Amount: ${Amount:F2} {Currency}", (decimal)payment.Amount / 100, payment.Currency?.ToUpper());
        _logger.Information("   - Status: {Status}", payment.Status);
        _logger.Information("   - Event Type: {EventType}", payment.EventType);

        // Simulate payment processing steps
        await UpdatePaymentStatus(payment);
        await SendCustomerNotification(payment);
        await UpdateAccountingSystem(payment);
        await TriggerFulfillment(payment);
    }

    private async Task UpdatePaymentStatus(PaymentProcessedContract payment)
    {
        await Task.Delay(50);
        _logger.Debug("📊 Updated payment status in database for {PaymentId}", payment.PaymentId);
    }

    private async Task SendCustomerNotification(PaymentProcessedContract payment)
    {
        await Task.Delay(30);
        _logger.Debug("📧 Sent payment confirmation to customer for {PaymentId}", payment.PaymentId);
    }

    private async Task UpdateAccountingSystem(PaymentProcessedContract payment)
    {
        await Task.Delay(40);
        _logger.Debug("💼 Updated accounting system for payment {PaymentId}", payment.PaymentId);
    }

    private async Task TriggerFulfillment(PaymentProcessedContract payment)
    {
        await Task.Delay(20);
        _logger.Debug("📦 Triggered fulfillment process for payment {PaymentId}", payment.PaymentId);
    }
}

/// <summary>
/// Custom exceptions for payment processing
/// </summary>
public class PaymentDeclinedException : Exception
{
    public PaymentDeclinedException(string message) : base(message) { }
}

public class FraudException : Exception
{
    public FraudException(string message) : base(message) { }
}

public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message) : base(message) { }
}

/// <summary>
/// PaymentErrorConsumer handles all payment processing errors
/// Automatically receives failed PaymentProcessedContract messages from Advanced mode
/// </summary>
public class PaymentErrorConsumer : IErrorConsumer<PaymentProcessedContract>
{
    private readonly IDbpeMessageBus _messageBus;
    private readonly ILogger _logger;

    public PaymentErrorConsumer(IDbpeMessageBus messageBus, ILogger logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task Handle(ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        var payment = errorMessage.OriginalMessage;
        
        _logger.Warning("🚨 Payment Error Handler - Processing failed payment {PaymentId}", payment.PaymentId);
        _logger.Warning("   Error Type: {ErrorType}", errorMessage.ErrorType);
        _logger.Warning("   Error Message: {ErrorMessage}", errorMessage.Message);
        _logger.Warning("   Retry Count: {RetryCount}", errorMessage.RetryCount);
        _logger.Warning("   Database Error ID: {ErrorId}", errorMessage.ErrorId);

        try
        {
            await ProcessPaymentError(errorMessage);
            _logger.Information("✅ Payment error handled successfully for {PaymentId}", payment.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Failed to handle payment error for {PaymentId}", payment.PaymentId);
            throw;
        }
    }

    private async Task ProcessPaymentError(ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        var payment = errorMessage.OriginalMessage;
        var errorType = errorMessage.ErrorType;

        switch (errorType)
        {
            case "DBPE.Example.Consumers.PaymentDeclinedException":
                await HandlePaymentDeclined(errorMessage);
                break;
                
            case "DBPE.Example.Consumers.FraudException":
                await HandleFraudDetected(errorMessage);
                break;
                
            case "System.TimeoutException":
                await HandleTimeout(errorMessage);
                break;
                
            case "DBPE.Example.Consumers.ExternalServiceException":
                await HandleExternalServiceFailure(errorMessage);
                break;
                
            default:
                await HandleGenericPaymentError(errorMessage);
                break;
        }
    }

    private async Task HandlePaymentDeclined(ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        var payment = errorMessage.OriginalMessage;
        
        _logger.Information("💳 Handling declined payment {PaymentId}", payment.PaymentId);
        
        // Reverse any partial transactions
        await ReversePartialTransaction(payment);
        
        // Send customer notification about declined payment
        await SendDeclinedPaymentNotification(payment);
        
        // Update order status
        await UpdateOrderStatus(payment.PaymentId, "PaymentDeclined");
        
        _logger.Information("✅ Declined payment {PaymentId} handled - Customer notified, transaction reversed", payment.PaymentId);
    }

    private async Task HandleFraudDetected(ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        var payment = errorMessage.OriginalMessage;
        
        _logger.Warning("🔒 Handling fraud-flagged payment {PaymentId}", payment.PaymentId);
        
        // Immediately freeze the transaction
        await FreezeTransaction(payment);
        
        // Alert fraud team
        await AlertFraudTeam(payment, errorMessage);
        
        // Put payment on hold
        await UpdateOrderStatus(payment.PaymentId, "FraudHold");
        
        _logger.Warning("🔒 Fraud-flagged payment {PaymentId} handled - Transaction frozen, fraud team alerted", payment.PaymentId);
    }

    private async Task HandleTimeout(ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        var payment = errorMessage.OriginalMessage;
        
        _logger.Information("⏰ Handling timeout for payment {PaymentId} (retry {RetryCount})", 
            payment.PaymentId, errorMessage.RetryCount);

        if (errorMessage.RetryCount < 3)
        {
            // Schedule retry with exponential backoff
            var delay = TimeSpan.FromMinutes(Math.Pow(2, errorMessage.RetryCount));
            _logger.Information("🔄 Scheduling retry for payment {PaymentId} in {Delay} minutes", 
                payment.PaymentId, delay.TotalMinutes);
            
            // In a real implementation, you'd use a job scheduler for this
            await Task.Delay(1000); // Simulate scheduling
            await _messageBus.Send(payment, "payments");
        }
        else
        {
            // Max retries exceeded
            _logger.Warning("⚠️ Max retries exceeded for payment {PaymentId}, marking for manual review", payment.PaymentId);
            await SendToManualReview(payment, errorMessage);
        }
    }

    private async Task HandleExternalServiceFailure(ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        var payment = errorMessage.OriginalMessage;
        
        _logger.Warning("🌐 Handling external service failure for payment {PaymentId}", payment.PaymentId);
        
        // Check service status
        await CheckPaymentGatewayStatus();
        
        // Queue for retry when service is available
        await QueueForServiceRecovery(payment);
        
        // Notify customer of delay
        await SendServiceDelayNotification(payment);
        
        _logger.Information("✅ External service failure handled for payment {PaymentId} - Queued for retry", payment.PaymentId);
    }

    private async Task HandleGenericPaymentError(ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        var payment = errorMessage.OriginalMessage;
        
        _logger.Warning("❓ Handling generic payment error for {PaymentId}: {ErrorType}", 
            payment.PaymentId, errorMessage.ErrorType);
        
        // Log detailed error information for investigation
        _logger.Error("📋 Payment Error Details:");
        _logger.Error("   Payment ID: {PaymentId}", payment.PaymentId);
        _logger.Error("   Amount: ${Amount:F2}", (decimal)payment.Amount / 100);
        _logger.Error("   Error: {ErrorMessage}", errorMessage.Message);
        _logger.Error("   Stack Trace: {StackTrace}", errorMessage.StackTrace);
        
        // Send to manual investigation
        await SendToManualReview(payment, errorMessage);
        
        _logger.Information("✅ Generic payment error logged and sent to manual review for {PaymentId}", payment.PaymentId);
    }

    // Helper methods for error handling actions
    private async Task ReversePartialTransaction(PaymentProcessedContract payment)
    {
        await Task.Delay(100);
        _logger.Debug("🔄 Reversed partial transaction for payment {PaymentId}", payment.PaymentId);
    }

    private async Task SendDeclinedPaymentNotification(PaymentProcessedContract payment)
    {
        await Task.Delay(50);
        _logger.Debug("📧 Sent declined payment notification for {PaymentId}", payment.PaymentId);
    }

    private async Task UpdateOrderStatus(string paymentId, string status)
    {
        await Task.Delay(30);
        _logger.Debug("📊 Updated order status to {Status} for payment {PaymentId}", status, paymentId);
    }

    private async Task FreezeTransaction(PaymentProcessedContract payment)
    {
        await Task.Delay(20);
        _logger.Debug("🔒 Froze transaction for payment {PaymentId}", payment.PaymentId);
    }

    private async Task AlertFraudTeam(PaymentProcessedContract payment, ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        await Task.Delay(100);
        _logger.Debug("🚨 Alerted fraud team about payment {PaymentId}", payment.PaymentId);
    }

    private async Task CheckPaymentGatewayStatus()
    {
        await Task.Delay(200);
        _logger.Debug("🌐 Checked payment gateway status");
    }

    private async Task QueueForServiceRecovery(PaymentProcessedContract payment)
    {
        await Task.Delay(50);
        _logger.Debug("⏳ Queued payment {PaymentId} for service recovery", payment.PaymentId);
    }

    private async Task SendServiceDelayNotification(PaymentProcessedContract payment)
    {
        await Task.Delay(50);
        _logger.Debug("📧 Sent service delay notification for payment {PaymentId}", payment.PaymentId);
    }

    private async Task SendToManualReview(PaymentProcessedContract payment, ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        await Task.Delay(100);
        _logger.Debug("👥 Sent payment {PaymentId} to manual review queue", payment.PaymentId);
    }
}
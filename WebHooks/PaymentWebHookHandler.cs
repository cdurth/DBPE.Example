using DBPE.WebHooks.Abstractions;
using DBPE.WebHooks.Attributes;
using DBPE.WebHooks.Models;
using DBPE.Messaging.Abstractions;
using DBPE.Example.Contracts;
using Newtonsoft.Json;
using Serilog;
using Rebus.Bus;

namespace DBPE.Example.WebHooks;

[WebHookHandler("payment/stripe", Description = "Handles Stripe payment webhooks")]
public class PaymentWebHookHandler : IWebHookHandler
{
    public string Id => "stripe-payment-webhook";
    public string Description => "Processes Stripe payment webhook notifications";
    public string Path => "payment/stripe";

    private readonly IDbpeMessageBus _messageBus;
    private readonly IBus _rebusBus;
    private readonly ILogger _logger;

    public PaymentWebHookHandler(IDbpeMessageBus messageBus, IBus rebusBus, ILogger logger)
    {
        _messageBus = messageBus;
        _rebusBus = rebusBus;
        _logger = logger;
    }

    public async Task<WebHookResult> Handle(WebHookRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Information("Processing Stripe webhook for event");

            if (string.IsNullOrEmpty(request.Body))
            {
                return WebHookResult.BadRequest("Empty request body");
            }

            var webhookData = JsonConvert.DeserializeObject<StripeWebHookData>(request.Body);
            if (webhookData == null)
            {
                return WebHookResult.BadRequest("Invalid JSON payload");
            }

            var paymentContract = new PaymentProcessedContract
            {
                PaymentId = webhookData.Data?.Object?.Id ?? "unknown",
                Amount = webhookData.Data?.Object?.Amount ?? 0,
                Currency = webhookData.Data?.Object?.Currency ?? "usd",
                Status = webhookData.Data?.Object?.Status ?? "unknown",
                EventType = webhookData.Type ?? "unknown",
                ProcessedAt = DateTime.UtcNow
            };

            // Send message with correlation ID if available
            if (request.CorrelationId.HasValue)
            {
                _logger.Information("Sending payment contract with correlation ID {CorrelationId}", request.CorrelationId.Value);
                
                // Add correlation ID to message metadata
                paymentContract.Metadata["CorrelationId"] = request.CorrelationId.Value.ToString();
                
                // Also store original correlation string if it exists
                if (request.Headers.TryGetValue("X-Correlation-ID", out var originalCorrelationId))
                {
                    paymentContract.Metadata["OriginalCorrelationId"] = originalCorrelationId;
                    _logger.Information("Added both GUID correlation ID {CorrelationId} and original string {OriginalCorrelationId} to metadata", 
                        request.CorrelationId.Value, originalCorrelationId);
                }
                else
                {
                    _logger.Information("Added correlation ID to metadata: {CorrelationId}", request.CorrelationId.Value);
                }
            }
            else
            {
                _logger.Information("Sending payment contract without correlation ID - request.CorrelationId is null");
            }

            await _messageBus.Send(paymentContract);

            _logger.Information("Stripe webhook processed successfully for payment: {PaymentId}", paymentContract.PaymentId);
            
            var result = WebHookResult.Ok("Webhook processed successfully");
            return result.WithCorrelation(request.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing Stripe webhook");
            return WebHookResult.InternalServerError($"Processing failed: {ex.Message}");
        }
    }
}

public class StripeWebHookData
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public StripeWebHookEventData? Data { get; set; }
}

public class StripeWebHookEventData
{
    public StripePaymentObject? Object { get; set; }
}

public class StripePaymentObject
{
    public string? Id { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
}


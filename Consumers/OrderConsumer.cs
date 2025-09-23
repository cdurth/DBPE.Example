using DBPE.Example.Contracts;
using DBPE.Messaging.Abstractions;
using DBPE.Messaging.Attributes;
using DBPE.Messaging.Configuration;
using Serilog;

namespace DBPE.Example.Consumers;

[QueueName("orders")]
[ConsumerConfig(PrefetchCount = 10, ConcurrentLimit = 5, Durable = true)]
[RetryPolicy(MaxRetries = 3, DelayInSeconds = 5, ExponentialBackoff = true)]
[ErrorHandling(Mode = ErrorHandlingMode.Simple)]
public class OrderConsumer : IConsumer<OrderContract>
{
    private readonly IDbpeMessageBus _messageBus;
    private readonly ILogger _logger;

    public OrderConsumer(IDbpeMessageBus messageBus, ILogger logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }


    public async Task Handle(OrderContract message, CancellationToken cancellationToken = default)
    {        
        _logger.Information("Processing order {OrderId} for customer {CustomerName} with amount {Amount:C}", 
            message.OrderId, message.CustomerName, message.Amount);

        try
        {
            // Simulate order processing
            await ProcessOrder(message, cancellationToken);

            // Publish order processed event
            var processedEvent = new OrderProcessedEvent
            {
                OrderId = message.OrderId,
                Status = "Processed",
                ProcessedAt = DateTime.UtcNow,
                ProcessedBy = "OrderConsumer"
            };

            await _messageBus.Publish(processedEvent, cancellationToken);

            _logger.Information("Order {OrderId} processed successfully", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to process order {OrderId}", message.OrderId);
            throw; // Will trigger retry policy
        }
    }

    private async Task ProcessOrder(OrderContract order, CancellationToken cancellationToken)
    {
        // Simulate processing time
        await Task.Delay(100, cancellationToken);

        // Validate order
        if (order.Amount <= 0)
            throw new InvalidOperationException($"Invalid order amount: {order.Amount}");

        if (order.Items.Count == 0)
            throw new InvalidOperationException("Order must contain at least one item");

        // Process each item
        foreach (var item in order.Items)
        {
            _logger.Debug("Processing item {ProductCode} x{Quantity}", item.ProductCode, item.Quantity);
            
            // Simulate item processing
            await Task.Delay(10, cancellationToken);
        }
    }
}
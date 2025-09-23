using DBPE.FileWatcher.Abstractions;
using DBPE.FileWatcher.Attributes;
using DBPE.Example.Contracts;
using DBPE.Messaging.Abstractions;
using Newtonsoft.Json;
using Serilog;

namespace DBPE.Example.FileWatchers;

/// <summary>
/// File watcher that processes order files and publishes OrderContract messages
/// </summary>
[FileWatcherConfig(
    Enabled = true,
    Archive = true,
    ArchiveDirectory = "ProcessedOrders",
    RetryCount = 3,
    RetryDelayMs = 2000,
    ProcessExistingFiles = true)]
[IncludeSubdirectories(false)]
public class OrderFileWatcher : IFileWatcher
{
    private readonly IDbpeMessageBus _messageBus;
    private readonly ILogger _logger;

    public string Description => "Processes order JSON files and publishes order contracts";
    public string Directory { get; set; } = @"C:\Temp\Orders";
    public string Filter { get; set; } = "*.json";
    public Dictionary<string, string> Options { get; set; } = new();

    public OrderFileWatcher(IDbpeMessageBus messageBus, ILogger logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task Execute(FileInfo file)
    {
        try
        {
            _logger.Information("Processing order file: {FileName}", file.Name);

            // Read the file content
            var jsonContent = await File.ReadAllTextAsync(file.FullName);
            
            // Parse the order data
            var orderData = JsonConvert.DeserializeObject<OrderFileData>(jsonContent);
            
            if (orderData == null)
            {
                throw new InvalidOperationException("Failed to deserialize order data");
            }

            // Create order contract
            var orderContract = new OrderContract
            {
                OrderId = int.Parse(orderData.OrderId),
                CustomerName = orderData.CustomerName,
                Amount = orderData.Amount,
                Items = orderData.Items?.Select(i => new OrderItem 
                { 
                    ProductCode = i.ProductId, 
                    ProductName = i.ProductId, // Use ProductId as name for simplicity
                    Quantity = i.Quantity, 
                    Price = i.Price 
                }).ToList() ?? new List<OrderItem>(),
                OrderDate = orderData.OrderDate ?? DateTime.UtcNow
            };

            // Publish to the message bus
            await _messageBus.Send(orderContract);
            
            _logger.Information("Successfully processed order file {FileName} - Order ID: {OrderId}", 
                file.Name, orderContract.OrderId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to process order file {FileName}", file.Name);
            throw;
        }
    }
}

/// <summary>
/// Data structure for order file content
/// </summary>
public class OrderFileData
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? OrderDate { get; set; }
    public List<OrderFileItem>? Items { get; set; }
}

/// <summary>
/// Data structure for order items in file
/// </summary>
public class OrderFileItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
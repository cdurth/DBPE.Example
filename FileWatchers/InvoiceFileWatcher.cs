using DBPE.FileWatcher.Abstractions;
using DBPE.FileWatcher.Attributes;
using DBPE.Messaging.Abstractions;
using DBPE.Example.Contracts;
using Newtonsoft.Json;
using Serilog;

namespace DBPE.Example.FileWatchers;

[FileWatcherConfig(Enabled = true, Archive = true, ArchiveDirectory = "ProcessedInvoices")]
public class InvoiceFileWatcher : IFileWatcher
{
    public string Id => "invoice-file-watcher";
    public string Description => "Processes invoice JSON files and sends them to message queue";
    public string Directory { get; set; } = "./WatchedFiles/Invoices";
    public string Filter { get; set; } = "*.json";
    public Dictionary<string, string> Options { get; set; } = new();

    private readonly IDbpeMessageBus _messageBus;
    private readonly ILogger _logger;

    public InvoiceFileWatcher(IDbpeMessageBus messageBus, ILogger logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task Execute(FileInfo file)
    {
        try
        {
            _logger.Information("Processing invoice file: {FileName}", file.Name);

            // Read and parse the JSON file
            var jsonContent = await File.ReadAllTextAsync(file.FullName);
            var invoiceData = JsonConvert.DeserializeObject<InvoiceFileData>(jsonContent);

            if (invoiceData == null)
            {
                _logger.Warning("Failed to parse invoice file: {FileName} - Invalid JSON", file.Name);
                return;
            }

            // Create the contract message
            var invoiceContract = new InvoiceProcessedContract
            {
                InvoiceId = invoiceData.InvoiceId,
                CustomerName = invoiceData.CustomerName,
                Amount = invoiceData.Amount,
                InvoiceDate = invoiceData.InvoiceDate,
                Items = invoiceData.Items?.Select(item => new InvoiceItemContract
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Total = item.Total
                }).ToList() ?? new List<InvoiceItemContract>(),
                ProcessedAt = DateTime.UtcNow,
                SourceFile = file.Name
            };

            // Send to message bus
            await _messageBus.Send(invoiceContract);

            _logger.Information("Invoice file processed successfully: {FileName} - Invoice ID: {InvoiceId}", 
                file.Name, invoiceData.InvoiceId);
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to parse JSON in invoice file: {FileName}", file.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing invoice file: {FileName}", file.Name);
            throw;
        }
    }
}

// Data models for file parsing
public class InvoiceFileData
{
    public string InvoiceId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public List<InvoiceItemData>? Items { get; set; }
}

public class InvoiceItemData
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DBPE.Example.Services;

/// <summary>
/// Periodically generates sample files for the file watcher to process
/// </summary>
public class FileGenerationService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(2); // Generate files every 2 minutes

    public FileGenerationService(ILogger logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 10 seconds before starting to let the app fully initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateSampleFile();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error generating sample file");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task GenerateSampleFile()
    {
        try
        {
            var sourceFile = Path.Combine("SampleData", "sample-invoice.json");
            var targetFile = Path.Combine("./WatchedFiles/Invoices", $"invoice-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            
            if (File.Exists(sourceFile))
            {
                await File.WriteAllTextAsync(targetFile, await File.ReadAllTextAsync(sourceFile));
                _logger.Information("Generated invoice file: {FileName}", Path.GetFileName(targetFile));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating sample file");
        }
    }
}
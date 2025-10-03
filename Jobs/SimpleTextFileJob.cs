using DBPE.JobScheduler.Abstractions;
using DBPE.JobScheduler.Attributes;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DBPE.Example.Jobs;

/// <summary>
/// Simple job that reads configuration and writes to a text file every minute
/// </summary>
[AutoSchedule(CronExpression = "0 * * * * ?", Enabled = true)]
public class SimpleTextFileJob : IDbpeJob
{
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public string JobId => "SimpleTextFileJob";
    public string Description => "Writes current timestamp to a configured file path every 5 minutes";
    public string Group => "Example";

    public SimpleTextFileJob(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Read the output path from job data (comes from configuration)
            var outputPath = context.JobData.ContainsKey("OutputPath")
                ? context.JobData["OutputPath"]?.ToString()
                : "./output.txt";

            // Read optional message from config
            var message = context.JobData.ContainsKey("Message")
                ? context.JobData["Message"]?.ToString()
                : "Job executed successfully";

            _logger.Information("SimpleTextFileJob executing - writing to {OutputPath}", outputPath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.Debug("Created directory: {Directory}", directory);
            }

            // Generate content
            var content = $"""
                ================================================
                DBPE Simple Job Execution Report
                ================================================
                Execution ID:       {context.ExecutionId}
                Job ID:             {JobId}
                Scheduled Time:     {context.ScheduledFireTime:yyyy-MM-dd HH:mm:ss}
                Actual Time:        {context.ActualFireTime:yyyy-MM-dd HH:mm:ss}
                Next Fire Time:     {context.NextFireTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}
                Previous Fire Time: {context.PreviousFireTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}
                Execution Count:    {context.RefireCount}
                Is Recovery:        {context.IsRecovery}
                ================================================
                Message: {message}
                ================================================
                Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}

                """;

            // Write to file
            await File.WriteAllTextAsync(outputPath, content, cancellationToken);

            _logger.Information("SimpleTextFileJob completed - wrote {Size} bytes to {OutputPath}",
                content.Length, outputPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "SimpleTextFileJob failed during execution");
            throw;
        }
    }
}

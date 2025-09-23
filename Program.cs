using DBPE.Core;
using DBPE.Example.Contracts;
using DBPE.Example.Services;
using DBPE.FileWatcher;
using DBPE.JobScheduler;
using DBPE.WebHooks;
using DBPE.WebServer;
using DBPE.Messaging;
using DBPE.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Reflection;

namespace DBPE.Example
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/dbpe-example-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting DBPE Example Application");

                // Create the DBPE engine
                var engine = EngineFactory.CreateBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddAssembly(Assembly.GetExecutingAssembly()) // Include this assembly for consumer discovery
                    .UseMessaging(options =>
                    {
                        // For now, use in-memory transport for testing
                        // options.UseRabbitMQ = false; // Will use local queues
                        options.EnableRetries = true;
                        options.DefaultRetryCount = 3;
                    })
                    .UseFileWatcher()
                    .UseJobScheduler()
                    .UseWebHooks(options =>
                    {
                        // Enable security for webhooks
                        options.EnableSecurity = true;
                    })
                    .UseWebServer(options =>
                    {
                        options.Port = 5000;
                        options.Host = "localhost";
                        options.EnableSwagger = true;
                        options.EnableRequestLogging = true;

                    })
                    .ConfigureServices((context, services) =>
                    {
                        // Register file generation service
                        services.AddHostedService<FileGenerationService>();
                    })
                    .Build();

                // Start the engine
                await engine.StartAsync();

                Log.Information("DBPE Engine started successfully");
                Log.Information("Web server: http://localhost:5000 | Swagger: http://localhost:5000/swagger");

                // Setup demo environment
                await SetupDemoEnvironment();

                // Keep the application running
                Log.Information("Press Ctrl+C to stop the application");
                var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cancellationTokenSource.Cancel();
                    Log.Information("Shutdown requested");
                };

                await Task.Delay(-1, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Log.Information("Application stopped gracefully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.Information("Shutting down DBPE Example Application");
                Log.CloseAndFlush();
            }
        }

        // EF Core will use this method for design-time operations (migrations)
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Only register services needed for EF migrations
                    // Don't start DBPE engine here - this is for design-time only
                });

        static Task SetupDemoEnvironment()
        {
            try
            {
                // Create directories if they don't exist
                var watchDir = "./WatchedFiles/Invoices";
                var archiveDir = Path.Combine(watchDir, "ProcessedInvoices");
                var reportsDir = "./Reports";

                Directory.CreateDirectory(watchDir);
                Directory.CreateDirectory(archiveDir);
                Directory.CreateDirectory(reportsDir);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting up demo environment");
                return Task.CompletedTask;
            }
        }
    }
}


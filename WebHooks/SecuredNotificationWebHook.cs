using DBPE.WebHooks.Abstractions;
using DBPE.WebHooks.Models;
using Newtonsoft.Json;
using Serilog;

namespace DBPE.Example.WebHooks;

public class SecuredNotificationWebHook : IWebHookHandler
{
    private readonly ILogger _logger;

    public SecuredNotificationWebHook(ILogger logger)
    {
        _logger = logger;
    }

    public string Id => "secured-notification";
    public string Description => "Secured webhook for receiving notifications (requires API key)";
    public string Path => "secured/notification";

    public async Task<WebHookResult> Handle(WebHookRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Information("Received secured notification webhook request for path {Path}",
                request.Path);

            // Log the authenticated API key (from headers)
            if (request.Headers.TryGetValue("X-API-Key", out var apiKey))
            {
                _logger.Information("Request authenticated with API key: {ApiKeyPrefix}...",
                    apiKey?.Substring(0, Math.Min(10, apiKey?.Length ?? 0)));
            }

            // Parse the notification data
            var notification = JsonConvert.DeserializeObject<NotificationData>(request.Body);

            if (notification == null)
            {
                _logger.Warning("Invalid notification data received");
                return WebHookResult.BadRequest("Invalid notification data");
            }

            _logger.Information("Processing notification: Type={Type}, Priority={Priority}, Message={Message}",
                notification.Type, notification.Priority, notification.Message);

            // Simulate processing based on notification type
            var processingResult = notification.Type?.ToLower() switch
            {
                "alert" => await ProcessAlert(notification),
                "info" => await ProcessInfo(notification),
                "warning" => await ProcessWarning(notification),
                _ => "Unknown notification type"
            };

            var response = new
            {
                success = true,
                receivedAt = DateTime.UtcNow,
                notificationId = Guid.NewGuid().ToString(),
                type = notification.Type,
                processingResult = processingResult,
                message = $"Notification processed successfully"
            };

            return WebHookResult.Ok(JsonConvert.SerializeObject(response, Formatting.Indented));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing secured notification webhook");

            return WebHookResult.InternalServerError("Failed to process notification");
        }
    }

    private Task<string> ProcessAlert(NotificationData notification)
    {
        _logger.Warning("ALERT received: {Message} (Priority: {Priority})",
            notification.Message, notification.Priority);
        return Task.FromResult($"Alert logged and escalated to monitoring team");
    }

    private Task<string> ProcessInfo(NotificationData notification)
    {
        _logger.Information("INFO notification: {Message}", notification.Message);
        return Task.FromResult($"Information notification logged");
    }

    private Task<string> ProcessWarning(NotificationData notification)
    {
        _logger.Warning("WARNING received: {Message} (Priority: {Priority})",
            notification.Message, notification.Priority);
        return Task.FromResult($"Warning logged for review");
    }
}

public class NotificationData
{
    public string Type { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
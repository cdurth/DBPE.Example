using DBPE.WebHooks.Correlation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DBPE.Example.Controllers;

[ApiController]
[Route("test/completion")]
public class CompletionTestController : ControllerBase
{
    private readonly ICompletionNotificationService _notificationService;
    private readonly IWebHookCorrelationStore _correlationStore;
    private readonly ILogger<CompletionTestController> _logger;

    public CompletionTestController(
        ICompletionNotificationService notificationService,
        IWebHookCorrelationStore correlationStore,
        ILogger<CompletionTestController> logger)
    {
        _notificationService = notificationService;
        _correlationStore = correlationStore;
        _logger = logger;
    }

    [HttpPost("process-pending")]
    public async Task<IActionResult> ProcessPendingNotifications()
    {
        try
        {
            _logger.LogInformation("🔧 Manually triggering completion notification processing");
            
            await _notificationService.ProcessPendingNotifications();
            
            return Ok(new { message = "Completion notification processing triggered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing pending notifications");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("correlations")]
    public async Task<IActionResult> GetAllCorrelations()
    {
        try
        {
            var pendingCorrelations = await _correlationStore.GetPendingNotifications();
            var correlationsList = pendingCorrelations.ToList();
            
            _logger.LogInformation("📋 Found {Count} pending correlations", correlationsList.Count);
            
            return Ok(new 
            { 
                count = correlationsList.Count,
                correlations = correlationsList.Select(c => new 
                {
                    correlationId = c.CorrelationId,
                    status = c.Status.ToString(),
                    receivedAt = c.ReceivedAt,
                    completedAt = c.CompletedAt,
                    retryCount = c.RetryCount,
                    hasNotificationConfig = c.NotificationConfig != null,
                    callbackUrl = c.NotificationConfig?.CallbackUrl
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting correlations");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DBPE.Example.Controllers;

[ApiController]
[Route("test/callback")]
public class CallbackTestController : ControllerBase
{
    private readonly ILogger<CallbackTestController> _logger;
    
    // Store received callbacks in memory for testing
    private static readonly ConcurrentDictionary<string, CallbackInfo> _callbacks = new();
    
    public CallbackTestController(ILogger<CallbackTestController> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Endpoint to receive completion notifications from the correlation system
    /// </summary>
    [HttpPost("completion/{correlationId}")]
    public IActionResult ReceiveCompletionCallback(string correlationId, [FromBody] object payload)
    {
        _logger.LogInformation("Received completion callback for correlation ID: {CorrelationId}", correlationId);
        
        var payloadStr = payload?.ToString() ?? "null";
        _logger.LogInformation("Callback payload: {Payload}", payloadStr);
        
        var callbackInfo = new CallbackInfo
        {
            CorrelationId = correlationId,
            ReceivedAt = DateTime.UtcNow,
            Payload = payloadStr,
            Status = "completed"
        };
        
        _callbacks[correlationId] = callbackInfo;
        
        return Ok(new { message = "Callback received", correlationId });
    }
    
    /// <summary>
    /// Get all received callbacks (for testing)
    /// </summary>
    [HttpGet("list")]
    public IActionResult GetCallbacks()
    {
        return Ok(_callbacks.Values.OrderByDescending(c => c.ReceivedAt).ToList());
    }
    
    /// <summary>
    /// Get specific callback by correlation ID
    /// </summary>
    [HttpGet("{correlationId}")]
    public IActionResult GetCallback(string correlationId)
    {
        if (_callbacks.TryGetValue(correlationId, out var callback))
        {
            return Ok(callback);
        }
        return NotFound(new { message = "No callback received for this correlation ID yet" });
    }
    
    /// <summary>
    /// Clear all callbacks (for testing)
    /// </summary>
    [HttpDelete("clear")]
    public IActionResult ClearCallbacks()
    {
        _callbacks.Clear();
        return Ok(new { message = "All callbacks cleared" });
    }
    
    private class CallbackInfo
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public string Payload { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
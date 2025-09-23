using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace DBPE.Example.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebHookDemoController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public WebHookDemoController(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("test-secured-webhook")]
    public async Task<IActionResult> TestSecuredWebhook([FromBody] TestWebhookRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();

            // Prepare the notification payload
            var notification = new
            {
                type = request.NotificationType ?? "info",
                message = request.Message ?? "Test notification from demo controller",
                priority = request.Priority ?? "normal",
                metadata = new
                {
                    source = "WebHookDemoController",
                    testId = Guid.NewGuid().ToString(),
                    environment = "development"
                },
                timestamp = DateTime.UtcNow
            };

            var json = JsonConvert.SerializeObject(notification);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Build the webhook URL
            var webhookUrl = $"http://localhost:5000/webhooks/secured/notification";

            // Create the request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
            {
                Content = content
            };

            // Add the API key header
            if (!string.IsNullOrEmpty(request.ApiKey))
            {
                httpRequest.Headers.Add("X-API-Key", request.ApiKey);
                _logger.Information("Testing secured webhook with API key: {ApiKeyPrefix}...",
                    request.ApiKey.Substring(0, Math.Min(10, request.ApiKey.Length)));
            }
            else
            {
                _logger.Warning("Testing secured webhook without API key (should fail)");
            }

            // Send the request
            var response = await client.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.Information("Webhook response: StatusCode={StatusCode}, Body={Body}",
                response.StatusCode, responseBody);

            // Debug logging
            _logger.Information("=== WEBHOOK RESPONSE DEBUG ===");
            _logger.Information("Raw responseBody: {ResponseBody}", responseBody);
            _logger.Information("ResponseBody is null or empty: {IsEmpty}", string.IsNullOrWhiteSpace(responseBody));
            _logger.Information("ResponseBody length: {Length}", responseBody?.Length ?? 0);

            // Parse the response body as JSON if possible
            object parsedResponse;
            try
            {
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    // Parse as JObject first
                    var jObject = Newtonsoft.Json.Linq.JObject.Parse(responseBody);
                    _logger.Information("Successfully parsed as JSON. Type: {Type}", jObject?.GetType()?.FullName);

                    // Convert JObject to a regular Dictionary to avoid serialization issues with System.Text.Json
                    parsedResponse = jObject.ToObject<Dictionary<string, object>>();
                    _logger.Information("Converted to Dictionary<string, object>");

                    // Try to log the parsed content
                    try
                    {
                        var serialized = JsonConvert.SerializeObject(parsedResponse);
                        _logger.Information("Re-serialized parsedResponse: {Serialized}", serialized);
                    }
                    catch (Exception serEx)
                    {
                        _logger.Error(serEx, "Failed to re-serialize parsedResponse");
                    }
                }
                else
                {
                    parsedResponse = responseBody;
                    _logger.Information("ResponseBody was empty, using as-is");
                }
            }
            catch (Exception parseEx)
            {
                _logger.Error(parseEx, "Failed to parse as JSON, returning as string");
                // If it's not valid JSON, return as string
                parsedResponse = responseBody;
            }

            var result = new
            {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                webhookUrl = webhookUrl,
                apiKeyProvided = !string.IsNullOrEmpty(request.ApiKey),
                response = parsedResponse,
                timestamp = DateTime.UtcNow
            };

            // Log what we're about to return
            _logger.Information("About to return result: {Result}", JsonConvert.SerializeObject(result));
            _logger.Information("=== END WEBHOOK RESPONSE DEBUG ===");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error testing secured webhook");
            return StatusCode(500, new { error = "Failed to test webhook", message = ex.Message });
        }
    }

    [HttpGet("webhook-test-info")]
    public IActionResult GetWebhookTestInfo()
    {
        return Ok(new
        {
            message = "Webhook Security Demo",
            securedWebhookUrl = "http://localhost:5000/webhooks/secured/notification",
            validApiKeys = new[]
            {
                new { key = "whk_stripe_prod_12345abcdef", description = "Stripe Production Key", allowedPaths = new[] { "payment/stripe", "payment/refund" } },
                new { key = "whk_internal_abcdef67890", description = "Internal Systems Key", allowedPaths = new[] { "*" } },
                new { key = "whk_test_123456789", description = "Test Key", allowedPaths = new[] { "payment/stripe" } }
            },
            testEndpoint = "/api/WebHookDemo/test-secured-webhook",
            examplePayload = new
            {
                apiKey = "whk_internal_abcdef67890",
                notificationType = "alert",
                message = "Test alert message",
                priority = "high"
            },
            notes = new[]
            {
                "The webhook requires an API key in the X-API-Key header",
                "Use one of the valid API keys from appsettings.json",
                "The 'internal-systems' key has access to all paths",
                "Without a valid API key, the webhook will return 401 Unauthorized"
            }
        });
    }
}

public class TestWebhookRequest
{
    public string? ApiKey { get; set; }
    public string? NotificationType { get; set; }
    public string? Message { get; set; }
    public string? Priority { get; set; }
}
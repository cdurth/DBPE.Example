using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DBPE.Example.Contracts;
using DBPE.Messaging.Abstractions;
using System.Text.Json;

namespace DBPE.Example.Controllers;

[ApiController]
[Route("test")]
public class WebHookTestController : ControllerBase
{
    private readonly ILogger<WebHookTestController> _logger;

    public WebHookTestController(ILogger<WebHookTestController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("webhooks")]
    public IActionResult GetWebHookTestPage()
    {
        var html = @"<!DOCTYPE html>
<html>
<head>
    <title>DBPE Webhook Testing</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .container { max-width: 800px; margin: 0 auto; }
        .test-section { margin: 20px 0; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }
        .test-section h3 { margin-top: 0; color: #333; }
        textarea { width: 100%; height: 150px; margin: 10px 0; }
        input[type='text'] { width: 100%; padding: 8px; margin: 5px 0; }
        button { background: #007cba; color: white; padding: 10px 20px; border: none; border-radius: 3px; cursor: pointer; margin: 5px; }
        button:hover { background: #005a87; }
        .response { margin-top: 10px; padding: 10px; background: #f5f5f5; border-left: 4px solid #007cba; }
        .error { border-left-color: #d32f2f; background: #ffebee; }
        .success { border-left-color: #388e3c; background: #e8f5e8; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>DBPE Webhook Testing Interface</h1>
        <p>Test webhook endpoints with different API keys and payloads.</p>
        
        <div class='test-section'>
            <h3>Secured Webhook Demo Test</h3>
            <p><strong>Endpoint:</strong> /api/WebHookDemo/test-secured-webhook</p>
            <p><em>This tests the webhook demo controller with improved JSON parsing that converts JObject to Dictionary for proper serialization.</em></p>

            <label>API Key:</label>
            <select id='demoApiKey'>
                <option value='whk_stripe_prod_12345abcdef'>stripe-production (valid)</option>
                <option value='whk_internal_abcdef67890'>internal-systems (valid)</option>
                <option value='whk_test_123456789'>test-key (valid)</option>
                <option value='invalid-key'>invalid-key (should fail)</option>
                <option value=''>no API key (should fail)</option>
            </select>

            <label>Notification Type:</label>
            <select id='notificationType'>
                <option value='info'>info</option>
                <option value='alert'>alert</option>
                <option value='warning'>warning</option>
                <option value='error'>error</option>
            </select>

            <label>Message:</label>
            <input type='text' id='demoMessage' value='Test notification from webhook demo' placeholder='Enter message' />

            <label>Priority:</label>
            <select id='priority'>
                <option value='low'>low</option>
                <option value='normal' selected>normal</option>
                <option value='high'>high</option>
                <option value='critical'>critical</option>
            </select>

            <button onclick='testDemoWebhook()'>Test Demo Webhook</button>
            <div id='demoResponse' class='response' style='display:none;'></div>
        </div>

        <div class='test-section'>
            <h3>Payment Webhook Test (with Correlation)</h3>
            <p><strong>Endpoint:</strong> /webhooks/payment/stripe</p>
            <p><em>This tests the payment pipeline: webhook → PaymentConsumer → correlation completion</em></p>

            <label>API Key:</label>
            <select id='stripeApiKey'>
                <option value='whk_stripe_prod_12345abcdef'>stripe-production (valid)</option>
                <option value='whk_internal_abcdef67890'>internal-systems (valid)</option>
                <option value='whk_test_123456789'>test-key (valid)</option>
                <option value='invalid-key'>invalid-key (should fail)</option>
            </select>

            <label>Stripe Payment Data:</label>
            <textarea id='stripePayload'>{
  ""id"": ""evt_test_webhook"",
  ""type"": ""payment_intent.succeeded"",
  ""data"": {
    ""object"": {
      ""id"": ""pi_test_correlation_123"",
      ""amount"": 2000,
      ""currency"": ""usd"",
      ""status"": ""succeeded""
    }
  }
}</textarea>

            <label>Callback URL (for correlation testing):</label>
            <input type='text' id='callbackUrl' value='https://ntfy.sh/dbpewebhooktest' placeholder='Enter callback URL' />
            <small style='display:block; margin-bottom:10px; color:#666;'>Use https://ntfy.sh/dbpewebhooktest for real-time notifications, or http://localhost:5000/test/callback/completion/ for local testing</small>

            <button onclick='testPaymentWebhook()'>Test Payment Webhook</button>
            <button onclick='testPaymentWebhookWithCorrelation()'>Test Payment with Correlation</button>
            <div id='stripeResponse' class='response' style='display:none;'></div>
        </div>

        <div class='test-section'>
            <h3>Custom Webhook Test</h3>
            <label>Webhook Path:</label>
            <input type='text' id='customPath' placeholder='e.g., payment/stripe' />
            
            <label>API Key:</label>
            <input type='text' id='customApiKey' placeholder='Enter API key' />
            
            <label>Custom Payload:</label>
            <textarea id='customPayload'>{
  ""message"": ""test webhook"",
  ""timestamp"": ""2024-01-01T12:00:00Z""
}</textarea>
            
            <button onclick='testCustomWebhook()'>Test Custom Webhook</button>
            <div id='customResponse' class='response' style='display:none;'></div>
        </div>

        <div class='test-section'>
            <h3>Available API Keys</h3>
            <ul>
                <li><strong>whk_stripe_prod_12345abcdef:</strong> Stripe Production Key - Valid for payment/stripe, payment/refund paths</li>
                <li><strong>whk_internal_abcdef67890:</strong> Internal Systems Key - Valid for all paths (*)</li>
                <li><strong>whk_test_123456789:</strong> Test Key - Valid for payment/stripe path</li>
                <li><strong>invalid-key:</strong> Should return 401 Unauthorized</li>
            </ul>
            <p><em>Note: The demo webhook controller tests these API keys against the secured notification endpoint at /webhooks/secured/notification.</em></p>
        </div>
    </div>

    <script>
        function showResponse(elementId, message, isError = false) {
            const element = document.getElementById(elementId);
            element.innerHTML = message;
            element.className = 'response ' + (isError ? 'error' : 'success');
            element.style.display = 'block';
        }

        async function testDemoWebhook() {
            const apiKey = document.getElementById('demoApiKey').value;
            const notificationType = document.getElementById('notificationType').value;
            const message = document.getElementById('demoMessage').value;
            const priority = document.getElementById('priority').value;

            const requestBody = {
                apiKey: apiKey,
                notificationType: notificationType,
                message: message,
                priority: priority
            };

            try {
                const response = await fetch('/api/WebHookDemo/test-secured-webhook', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(requestBody)
                });

                const result = await response.json();
                const formattedResponse = `<strong>Status:</strong> ${response.status}<br>
                                          <strong>Success:</strong> ${result.success}<br>
                                          <strong>Webhook URL:</strong> ${result.webhookUrl}<br>
                                          <strong>API Key Provided:</strong> ${result.apiKeyProvided}<br>
                                          <strong>Response:</strong> <pre>${JSON.stringify(result.response, null, 2)}</pre>
                                          <strong>Timestamp:</strong> ${result.timestamp}`;
                showResponse('demoResponse', formattedResponse, !response.ok);
            } catch (error) {
                showResponse('demoResponse', `<strong>Error:</strong> ${error.message}`, true);
            }
        }

        async function testPaymentWebhook() {
            const apiKey = document.getElementById('stripeApiKey').value;
            const payload = document.getElementById('stripePayload').value;

            try {
                const response = await fetch('/webhooks/payment/stripe', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-API-Key': apiKey
                    },
                    body: payload
                });

                const result = await response.text();
                const message = `<strong>Status:</strong> ${response.status}<br><strong>Response:</strong> ${result}`;
                showResponse('stripeResponse', message, !response.ok);
            } catch (error) {
                showResponse('stripeResponse', `<strong>Error:</strong> ${error.message}`, true);
            }
        }

        async function testPaymentWebhookWithCorrelation() {
            const apiKey = document.getElementById('stripeApiKey').value;
            const payload = document.getElementById('stripePayload').value;
            const correlationId = 'payment-correlation-' + Date.now();

            // Get callback URL from input field, with fallback logic
            let callbackUrl = document.getElementById('callbackUrl').value.trim();
            if (!callbackUrl) {
                callbackUrl = window.location.origin + '/test/callback/completion/' + correlationId;
            } else if (callbackUrl.endsWith('/')) {
                // If URL ends with /, append correlation ID
                callbackUrl = callbackUrl + correlationId;
            } else if (!callbackUrl.includes('ntfy.sh') && !callbackUrl.includes(correlationId)) {
                // If it's a localhost-style URL without correlation ID, append it
                callbackUrl = callbackUrl + '/' + correlationId;
            }

            try {
                const response = await fetch('/webhooks/payment/stripe', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-API-Key': apiKey,
                        'X-Correlation-ID': correlationId,
                        'X-Completion-URL': callbackUrl
                    },
                    body: payload
                });

                const result = await response.text();
                const message = `<strong>🔄 Payment Pipeline Test</strong><br>
                                 <strong>Correlation ID:</strong> ${correlationId}<br>
                                 <strong>Callback URL:</strong> ${callbackUrl}<br>
                                 <strong>Webhook Status:</strong> ${response.status}<br>
                                 <strong>Response:</strong> ${result}<br>
                                 <em>Now check PaymentConsumer logs for correlation completion...</em><br>
                                 <button onclick=""checkCallback('${correlationId}')"">Check Callback Status</button>`;
                showResponse('stripeResponse', message, !response.ok);

                // Start polling for callback
                if (response.ok) {
                    setTimeout(() => checkCallback(correlationId), 2000);
                }
            } catch (error) {
                showResponse('stripeResponse', `<strong>Error:</strong> ${error.message}`, true);
            }
        }
        
        async function checkCallback(correlationId) {
            try {
                const response = await fetch('/test/callback/' + correlationId);
                if (response.ok) {
                    const callback = await response.json();
                    showResponse('stripeResponse', 
                        `<strong>✅ Callback Received!</strong><br>
                         <strong>Correlation ID:</strong> ${callback.correlationId}<br>
                         <strong>Received At:</strong> ${callback.receivedAt}<br>
                         <strong>Status:</strong> ${callback.status}<br>
                         <strong>Payload:</strong> <pre>${callback.payload}</pre>`, 
                        false);
                } else {
                    showResponse('stripeResponse', 
                        `<strong>⏳ Waiting for callback...</strong><br>
                         <strong>Correlation ID:</strong> ${correlationId}<br>
                         <button onclick=""checkCallback('${correlationId}')"">Check Again</button>`, 
                        false);
                }
            } catch (error) {
                showResponse('stripeResponse', `<strong>Error checking callback:</strong> ${error.message}`, true);
            }
        }

        async function testCustomWebhook() {
            const path = document.getElementById('customPath').value;
            const apiKey = document.getElementById('customApiKey').value;
            const payload = document.getElementById('customPayload').value;
            
            if (!path) {
                showResponse('customResponse', '<strong>Error:</strong> Please enter a webhook path', true);
                return;
            }
            
            try {
                const response = await fetch(`/webhooks/${path}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-API-Key': apiKey
                    },
                    body: payload
                });
                
                const result = await response.text();
                const message = `<strong>Status:</strong> ${response.status}<br><strong>Response:</strong> ${result}`;
                showResponse('customResponse', message, !response.ok);
            } catch (error) {
                showResponse('customResponse', `<strong>Error:</strong> ${error.message}`, true);
            }
        }
    </script>
</body>
</html>";

        return Content(html, "text/html");
    }
}
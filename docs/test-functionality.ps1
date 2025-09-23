# PowerShell script to test DBPE.Example functionality
param(
    [string]$SqlPassword = "YourStrong@Passw0rd"
)

Write-Host "🧪 DBPE.Example Functionality Test Script" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Check if application is running
$appRunning = $false
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/WebHookDemo/webhook-test-info" -Method GET -ErrorAction Stop
    $appRunning = $true
    Write-Host "✅ Application is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Application is not running. Please start it first with 'dotnet run'" -ForegroundColor Red
    Write-Host "   Expected at: http://localhost:5000" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "🔐 Testing Secured Webhooks..." -ForegroundColor Cyan

# Test 1: Valid API Key
Write-Host "Test 1: Valid API Key (should succeed)" -ForegroundColor Yellow
try {
    $validKeyPayload = @{
        apiKey = "whk_internal_abcdef67890"
        notificationType = "alert"
        message = "PowerShell test - valid key"
        priority = "high"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/WebHookDemo/test-secured-webhook" `
        -Method POST `
        -ContentType "application/json" `
        -Body $validKeyPayload `
        -ErrorAction Stop

    if ($response.success) {
        Write-Host "✅ Valid API key test passed" -ForegroundColor Green
    } else {
        Write-Host "❌ Valid API key test failed: $($response.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Valid API key test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Invalid API Key
Write-Host "Test 2: No API Key (should fail)" -ForegroundColor Yellow
try {
    $invalidKeyPayload = @{
        notificationType = "info"
        message = "PowerShell test - no key"
        priority = "normal"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/WebHookDemo/test-secured-webhook" `
        -Method POST `
        -ContentType "application/json" `
        -Body $invalidKeyPayload `
        -ErrorAction Stop

    if (-not $response.success) {
        Write-Host "✅ No API key test passed (correctly rejected)" -ForegroundColor Green
    } else {
        Write-Host "❌ No API key test failed (should have been rejected)" -ForegroundColor Red
    }
} catch {
    Write-Host "✅ No API key test passed (correctly rejected with error)" -ForegroundColor Green
}

# Test 3: Direct webhook call
Write-Host "Test 3: Direct webhook call with API key" -ForegroundColor Yellow
try {
    $directPayload = @{
        type = "warning"
        message = "PowerShell direct webhook test"
        priority = "medium"
        metadata = @{
            source = "PowerShell Test Script"
            testId = [System.Guid]::NewGuid().ToString()
        }
        timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    } | ConvertTo-Json

    $headers = @{
        "Content-Type" = "application/json"
        "X-API-Key" = "whk_internal_abcdef67890"
    }

    $response = Invoke-RestMethod -Uri "http://localhost:5000/webhooks/secured/notification" `
        -Method POST `
        -Headers $headers `
        -Body $directPayload `
        -ErrorAction Stop

    Write-Host "✅ Direct webhook call passed" -ForegroundColor Green
    Write-Host "   Response: $($response.success)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Direct webhook call failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "📁 Testing FileWatcher..." -ForegroundColor Cyan

# Create watched directory if it doesn't exist
$watchDir = "./WatchedFiles/Invoices"
if (-not (Test-Path $watchDir)) {
    New-Item -ItemType Directory -Path $watchDir -Force | Out-Null
    Write-Host "Created watch directory: $watchDir" -ForegroundColor Gray
}

# Create test invoice file
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$testInvoice = @{
    InvoiceId = "INV-TEST-$timestamp"
    CustomerName = "PowerShell Test Customer"
    Amount = 123.45
    InvoiceDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    Items = @(
        @{
            Description = "Test Item from PowerShell"
            Quantity = 1
            UnitPrice = 123.45
            Total = 123.45
        }
    )
} | ConvertTo-Json

$testFileName = "$watchDir/test-invoice-$timestamp.json"
$testInvoice | Out-File -FilePath $testFileName -Encoding UTF8

Write-Host "✅ Created test invoice file: $testFileName" -ForegroundColor Green
Write-Host "   Watch the logs for processing..." -ForegroundColor Yellow
Write-Host "   File should be moved to ProcessedInvoices folder after processing" -ForegroundColor Yellow

Write-Host ""
Write-Host "🌐 Testing Web Endpoints..." -ForegroundColor Cyan

# Test Swagger
try {
    $swaggerResponse = Invoke-WebRequest -Uri "http://localhost:5000/swagger" -ErrorAction Stop
    if ($swaggerResponse.StatusCode -eq 200) {
        Write-Host "✅ Swagger UI accessible at http://localhost:5000/swagger" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Swagger UI not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test webhook info endpoint
try {
    $webhookInfo = Invoke-RestMethod -Uri "http://localhost:5000/api/WebHookDemo/webhook-test-info" -Method GET -ErrorAction Stop
    Write-Host "✅ Webhook info endpoint working" -ForegroundColor Green
    Write-Host "   Available API keys: $($webhookInfo.validApiKeys.Count)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Webhook info endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "📊 Test Summary" -ForegroundColor Magenta
Write-Host "===============" -ForegroundColor Magenta
Write-Host "🔐 Webhook Security: Check logs above" -ForegroundColor White
Write-Host "📁 FileWatcher: Monitor logs for file processing" -ForegroundColor White
Write-Host "🌐 Web Endpoints: Check Swagger at http://localhost:5000/swagger" -ForegroundColor White
Write-Host "🗃️  SQL Server: Check database for messaging tables" -ForegroundColor White

Write-Host ""
Write-Host "💡 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Check application logs for file processing" -ForegroundColor White
Write-Host "   2. Visit http://localhost:5000/swagger to explore APIs" -ForegroundColor White
Write-Host "   3. Check SQL Server for messaging tables" -ForegroundColor White
Write-Host "   4. Use Bruno collection for additional API testing" -ForegroundColor White

Write-Host ""
Write-Host "🎉 Test script completed!" -ForegroundColor Green
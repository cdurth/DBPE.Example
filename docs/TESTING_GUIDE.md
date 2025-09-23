# DBPE.Example Testing Guide

This guide will help you test all the functionality in the DBPE.Example project.

## Prerequisites

### 1. SQL Server Setup
You need a SQL Server instance running. Update the connection strings in `appsettings.json`:

```json
"ConnectionString": "Server=localhost;Database=DBPE_Example;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=true;MultipleActiveResultSets=true;"
```

**Options:**
- Local SQL Server instance
- SQL Server Express (free)
- Docker: `docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest`

### 2. Build and Run
```bash
cd /path/to/DBPE.Example
dotnet build
dotnet run
```

The application will start on `http://localhost:5000`

## Testing Areas

### 1. 🚀 Application Startup Test
**What to verify:**
- Application starts without errors
- All modules initialize (WebHooks, FileWatcher, JobScheduler, WebServer, Messaging)
- SQL Server connection establishes
- Swagger UI available at `http://localhost:5000/swagger`

**Expected logs:**
```
[Information] Starting DBPE Example Application
[Information] Starting Messaging module...
[Information] Starting FileWatcher module...
[Information] Starting JobScheduler module...
[Information] Starting WebHooks module...
[Information] Starting WebServer module...
[Information] DBPE Engine started successfully
[Information] Web server: http://localhost:5000 | Swagger: http://localhost:5000/swagger
```

### 2. 🌐 Interactive Webhook Test Page
**Use the built-in test webpage for comprehensive webhook testing:**

#### Access Test Page
1. **Start the application:** `dotnet run`
2. **Open browser:** `http://localhost:5000/test/webhooks`

#### Test Features Available:
1. **Stripe Payment Webhook Testing**
   - Multiple pre-configured API keys (valid and invalid)
   - Real payment webhook payload testing
   - Correlation ID generation and tracking

2. **Callback URL Testing**
   - **Local Testing:** Uses built-in callback endpoint for immediate feedback
   - **External Testing:** Support for ntfy.sh or custom URLs
   - **Real-time Monitoring:** Check correlation status and completion

3. **Custom Webhook Testing**
   - Test any webhook path with custom payloads
   - Custom API key testing
   - Error scenario testing

#### Quick Test Steps:
1. Select "stripe-production (valid)" API key
2. Choose "Local Testing" for callback type
3. Click **"Test with Correlation ID"**
4. Watch for correlation ID generation and status updates
5. Click **"Check Local Callback"** to verify completion notification

### 3. 🔐 Manual Webhook Testing (Alternative)
**For command-line testing:**

#### Test 1: Valid API Key (Should Succeed)
```bash
curl -X POST http://localhost:5000/webhooks/payment/stripe \
  -H "Content-Type: application/json" \
  -H "X-API-Key: whk_stripe_prod_12345abcdef" \
  -d '{
    "id": "evt_test_webhook",
    "type": "payment_intent.succeeded",
    "data": {
      "object": {
        "id": "pi_test_123456",
        "amount": 2500,
        "currency": "usd",
        "status": "succeeded"
      }
    }
  }'
```

**Expected:** `200 OK` with success response

#### Test 2: With Correlation Tracking
```bash
curl -X POST http://localhost:5000/webhooks/payment/stripe \
  -H "Content-Type: application/json" \
  -H "X-API-Key: whk_stripe_prod_12345abcdef" \
  -H "X-Correlation-ID: $(uuidgen)" \
  -H "X-Completion-URL: http://localhost:5000/test/callback/completion/{correlationId}" \
  -d '{
    "id": "evt_test_correlation",
    "type": "payment_intent.succeeded",
    "data": {
      "object": {
        "id": "pi_correlation_test",
        "amount": 1500,
        "currency": "usd",
        "status": "succeeded"
      }
    }
  }'
```

**Expected:** `200 OK` with correlation tracking initiated

#### Test 3: Invalid API Key (Should Fail)
```bash
curl -X POST http://localhost:5000/webhooks/payment/stripe \
  -H "Content-Type: application/json" \
  -H "X-API-Key: invalid-key" \
  -d '{
    "id": "evt_test_fail",
    "type": "payment_intent.succeeded"
  }'
```

**Expected:** `401 Unauthorized`

### 4. 📁 FileWatcher Test
**Test automatic file processing:**

#### Create a test invoice file:
```bash
# Create the watched directory if it doesn't exist
mkdir -p ./WatchedFiles/Invoices

# Create a test invoice file
cat > ./WatchedFiles/Invoices/test-invoice-$(date +%s).json << 'EOF'
{
  "InvoiceId": "INV-2024-001",
  "CustomerName": "Test Customer",
  "Amount": 1299.99,
  "InvoiceDate": "2024-01-20T10:30:00Z",
  "Items": [
    {
      "Description": "Test Product",
      "Quantity": 2,
      "UnitPrice": 649.99,
      "Total": 1299.98
    }
  ]
}
EOF
```

**Expected behavior:**
1. File gets processed automatically
2. Logs show: `"Processing invoice file: test-invoice-XXXXX.json"`
3. File gets moved to `ProcessedInvoices` folder
4. Invoice message sent to message bus

### 5. 📊 Message Processing Test
**Test that consumers process messages:**

**Expected logs after FileWatcher processes a file:**
```
[Information] Processing invoice file: test-invoice-XXXXX.json
[Information] Invoice file processed successfully: test-invoice-XXXXX.json - Invoice ID: INV-2024-001
[Information] Received InvoiceProcessed message for Invoice INV-2024-001
```

### 6. 🗃️ SQL Server Persistence Test
**Verify database tables are created:**

Connect to your SQL Server and check these tables exist:
```sql
-- Messaging tables
SELECT * FROM sys.tables WHERE name LIKE 'messaging_%'

-- Should show tables like:
-- messaging_MessageTracking
-- messaging_FailedMessages
-- messaging_ContractTables
```

**Check webhook correlation tables:**
```sql
-- Webhook correlation schema and tables
SELECT * FROM sys.schemas WHERE name = 'webhook_correlations'
SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('webhook_correlations')

-- Should show:
-- webhook_correlations.correlations
```

**Check message tracking:**
```sql
-- View processed messages
SELECT TOP 10 * FROM messaging_MessageTracking
ORDER BY ProcessedAt DESC

-- View webhook correlations
SELECT TOP 10 * FROM webhook_correlations.correlations
ORDER BY received_at DESC
```

### 7. 🌐 Web Server Test
**Test API endpoints:**

#### Swagger UI
Visit: `http://localhost:5000/swagger`

#### Webhook Info Endpoint
```bash
curl http://localhost:5000/api/WebHookDemo/webhook-test-info
```

**Expected:** JSON with webhook configuration details

### 8. 📅 JobScheduler Test
**Verify scheduled jobs are configured:**

**Expected logs:**
```
[Information] JobScheduler module started successfully
[Information] Discovered 1 jobs: DailyReportJob
```

## Troubleshooting

### Common Issues

#### 1. SQL Server Connection Fails
```
Error: Failed to connect to SQL Server
```
**Solution:**
- Check SQL Server is running
- Verify connection string
- Check firewall settings

#### 2. Port 5000 Already in Use
```
Error: Failed to bind to address http://localhost:5000
```
**Solution:**
- Change port in appsettings.json or Program.cs
- Or stop other services using port 5000

#### 3. FileWatcher Not Processing Files
**Check:**
- Directory exists: `./WatchedFiles/Invoices`
- File permissions
- Logs for FileWatcher startup

#### 4. Webhook Returns 500 Internal Server Error
**Check:**
- API key configuration in appsettings.json
- Logs for detailed error messages

## Using Bruno API Collection

If you have Bruno installed:

1. Open Bruno
2. Open collection: `./DBPE.Bruno`
3. Run the test requests:
   - "Get Webhook Info"
   - "Test Secured Webhook - With Valid Key"
   - "Test Secured Webhook - Without Key"
   - "Direct Webhook Call - With API Key"

## Success Indicators

✅ **All modules start without errors**
✅ **Swagger UI loads at http://localhost:5000/swagger**
✅ **Interactive webhook test page loads at /test/webhooks**
✅ **Webhook correlation tracking works with SQL Server persistence**
✅ **Secured webhook accepts valid API keys and rejects invalid ones**
✅ **FileWatcher processes JSON files from the watched directory**
✅ **Message consumers log received messages**
✅ **SQL Server tables are created automatically (messaging + webhooks)**
✅ **API endpoints respond correctly**
✅ **Completion notifications work for correlated webhooks**

## Performance Test

For a quick end-to-end test:

```bash
# 1. Start the application
dotnet run

# 2. In another terminal, test the webhook
curl -X POST http://localhost:5000/api/WebHookDemo/test-secured-webhook \
  -H "Content-Type: application/json" \
  -d '{"apiKey": "whk_internal_abcdef67890", "notificationType": "alert", "message": "End-to-end test", "priority": "high"}'

# 3. Create a test file
echo '{"InvoiceId": "TEST-001", "CustomerName": "Test Customer", "Amount": 100.00, "InvoiceDate": "2024-01-20T10:30:00Z"}' > ./WatchedFiles/Invoices/test-$(date +%s).json

# 4. Check logs for processing
```

If both succeed, your DBPE.Example is fully functional! 🎉
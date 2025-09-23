# DBPE Example Project

> **📌 Important Note:** This is an example project for illustration purposes only. The DBPE core modules (DBPE.Core, DBPE.Messaging, DBPE.FileWatcher, DBPE.JobScheduler, DBPE.WebHooks, DBPE.WebServer) are required dependencies to build and run this example project. These modules are proprietary and must be obtained separately.

This example project demonstrates the complete functionality of the **DBPE (Durt Business Process Engine)** - a modern, event-driven business process engine built with .NET 8, Microsoft Dependency Injection, and Rebus messaging.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- SQL Server (local, Express, or Docker)

### Setup
1. Clone the repository
2. Update the connection string in `appsettings.json`
3. Run the application:
   ```bash
   dotnet run
   ```

### Testing
- **Interactive Test Page**: Navigate to `http://localhost:5000/test/webhooks`
- **Swagger UI**: Navigate to `http://localhost:5000/swagger`

For detailed setup and testing instructions, see the [docs](./docs) folder.

## 🚀 What This Example Demonstrates

The example showcases a real-world business scenario with:
- **Invoice Processing** via file monitoring
- **Automated Report Generation** via scheduled jobs
- **Message-driven Architecture** with consumers
- **Advanced Error Handling Demonstration** with both Simple and Advanced modes
- **Complete Integration** of all DBPE modules

## 📁 Project Structure

```
DBPE.Example/
├── Program.cs                    # Main application entry point
├── appsettings.json             # Configuration file
├── Contracts/                   # Message contracts
│   ├── InvoiceProcessedContract.cs
│   └── ReportGeneratedContract.cs
├── Consumers/                   # Message consumers
│   ├── InvoiceProcessedConsumer.cs
│   └── ReportGeneratedConsumer.cs
├── FileWatchers/               # File monitoring handlers
│   └── InvoiceFileWatcher.cs
├── Jobs/                       # Scheduled jobs
│   └── DailyReportJob.cs
└── WebHooks/                   # Webhook handlers
    └── PaymentWebHookHandler.cs
```

## 🔧 Components Breakdown

### 1. **File Watcher - Invoice Processing**
**File:** `FileWatchers/InvoiceFileWatcher.cs`

**What it does:**
- Monitors `./WatchedFiles/Invoices/` directory for new JSON files
- Automatically processes invoice files when they appear
- Parses JSON invoice data and converts to structured contracts
- Sends `InvoiceProcessedContract` messages to the message bus
- Archives processed files to prevent reprocessing

**Configuration:** `appsettings.json` → `FileWatchers.InvoiceFileWatcher`

**Demo:** The application automatically creates a sample invoice file after 3 seconds

### 2. **Scheduled Job - Daily Reports**
**File:** `Jobs/DailyReportJob.cs`

**What it does:**
- Runs every 5 minutes (configured for demo purposes)
- Generates simulated daily business reports
- Collects random metrics (record count, total amounts)
- Sends `ReportGeneratedContract` messages to the message bus
- Uses job data from configuration for output paths

**Configuration:** `appsettings.json` → `JobScheduler.DailyReportJob`

**Attributes:**
- `[AutoSchedule]` - Automatically scheduled on startup
- `[JobConfig]` - Job configuration and metadata

### 3. **Message Contracts**
**Files:** `Contracts/InvoiceProcessedContract.cs`, `Contracts/ReportGeneratedContract.cs`

**What they do:**
- Define the structure of messages flowing through the system
- Carry business data between components
- Support serialization and routing via Rebus

**InvoiceProcessedContract includes:**
- Invoice ID, Customer Name, Amount, Date
- Line items with descriptions and pricing
- Processing metadata (source file, timestamp)

**ReportGeneratedContract includes:**
- Report ID, Type, Generation timestamp
- Record counts and totals
- Job execution context

**PaymentProcessedContract includes:**
- Payment ID, Amount, Currency, Status
- Event type and processing timestamp
- Metadata for customer and order tracking

**NotificationRequest includes:**
- Recipient, Subject, Message content
- Notification type and priority
- Template data for personalization

### 4. **Message Consumers**
**Files:** `Consumers/InvoiceProcessedConsumer.cs`, `Consumers/ReportGeneratedConsumer.cs`

**What they do:**
- Listen for specific message types from the message bus
- Process business logic when messages arrive
- Log detailed information about received data
- Simulate downstream processing (email notifications, database updates)

**InvoiceProcessedConsumer:**
- Processes invoice completion messages
- Logs detailed invoice information including line items
- Simulates invoice validation and accounting integration

**ReportGeneratedConsumer:**
- Processes report generation messages
- Logs report metrics and metadata
- Simulates report distribution and archival

### 5. **Webhook Handler - Payment Processing**
**File:** `WebHooks/PaymentWebHookHandler.cs`

**What it does:**
- Handles incoming webhook requests from payment providers (e.g., Stripe)
- Processes payment notifications and status updates
- Converts webhook data to internal contracts
- Sends `PaymentProcessedContract` messages to the message bus

**Endpoint:** `POST /webhooks/payment/stripe`
**Test Page:** `GET /test/webhooks` (Interactive webhook testing interface)

## 🏥 Error Handling Demonstration

This example includes a comprehensive demonstration of DBPE.Messaging's error handling capabilities, showcasing both **Simple** and **Advanced** error handling modes in action.

### Simple Error Handling Mode

**Consumers:** `OrderConsumer`, `NotificationConsumer`

**How it works:**
- Errors are automatically routed to dead letter queues with "-error" suffix
- `OrderConsumer` errors → `orders-error` queue → `OrderErrorConsumer`
- `NotificationConsumer` errors → `notifications-error` queue → `NotificationErrorConsumer`
- Lightweight, high-performance error handling
- Immediate error visibility and processing

**Error Scenarios Demonstrated:**
- **Order Validation Errors**: Invalid amounts, missing items
- **Timeout Errors**: Simulated processing delays
- **Email Validation Errors**: Invalid email addresses
- **Rate Limiting**: Notification service rate limits
- **Template Errors**: Missing or invalid templates

### Advanced Error Handling Mode

**Consumer:** `PaymentConsumer`

**How it works:**
- Errors are stored in SQLite database (`messaging_errors.db`)
- Automatic forwarding to `PaymentErrorConsumer`
- Full audit trail with error status tracking
- Rich error metadata and searchable history

**Error Scenarios Demonstrated:**
- **Payment Declined**: Insufficient funds, invalid cards
- **Fraud Detection**: Suspicious payment patterns
- **External Service Failures**: Payment gateway unavailable
- **Timeout Errors**: Payment processing delays
- **Generic Errors**: Unexpected payment processing failures

### Error Consumer Examples

#### Simple Mode Error Consumer
```csharp
public class OrderErrorConsumer
{
    public async Task Handle(ErrorMessage<OrderContract> errorMessage)
    {
        var order = errorMessage.OriginalMessage;
        
        switch (errorMessage.ErrorType)
        {
            case "System.InvalidOperationException":
                await HandleValidationError(errorMessage);
                break;
            case "System.TimeoutException":
                await HandleTimeoutError(errorMessage);
                break;
            default:
                await HandleGenericError(errorMessage);
                break;
        }
    }
}
```

#### Advanced Mode Error Consumer
```csharp
public class PaymentErrorConsumer
{
    public async Task Handle(ErrorMessage<PaymentProcessedContract> errorMessage)
    {
        var payment = errorMessage.OriginalMessage;
        
        // Error is already stored in database with ID: errorMessage.ErrorId
        // Automatic status tracking: Pending → Processing → Completed/Failed
        
        switch (errorMessage.ErrorType)
        {
            case "PaymentDeclinedException":
                await HandlePaymentDeclined(errorMessage);
                break;
            case "FraudException":
                await HandleFraudDetected(errorMessage);
                break;
            // ... other specific error handlers
        }
    }
}
```

### Real-Time Error Generation

The `ErrorDemonstrationService` automatically generates test messages every 30-45 seconds:

- **Payment Messages**: Sent to `payments` queue (Advanced error handling)
- **Notification Messages**: Sent to `notifications` queue (Simple error handling)
- **Simulated Failures**: Random error scenarios (10-30% failure rate)

### Error Database Schema (Advanced Mode)

When using Advanced mode, errors are stored with:
- **Error ID**: Unique identifier for tracking
- **Original Message**: Full message content preserved
- **Error Details**: Exception type, message, stack trace
- **Consumer Info**: Which consumer failed, queue name
- **Timestamps**: When error occurred, when resolved
- **Status Tracking**: Pending → Processing → Completed/Failed

### Monitoring Error Handling

Watch the logs to see error handling in action:

```bash
# Simple Mode (Dead Letter Queue)
📨 Notification Error Handler - Processing failed notification to invalid-email
   Error Type: DBPE.Example.Consumers.InvalidEmailException
   Error Message: Invalid email address: invalid-email
   Retry Count: 0

# Advanced Mode (Database + Forwarding)
🚨 Payment Error Handler - Processing failed payment pay_abc123
   Error Type: DBPE.Example.Consumers.PaymentDeclinedException
   Error Message: Payment pay_abc123 declined: Insufficient funds
   Database Error ID: 550e8400-e29b-41d4-a716-446655440000
```

## ⚙️ Configuration

### `appsettings.json`

```json
{
  "FileWatchers": {
    "InvoiceFileWatcher": {
      "Enabled": true,
      "Directory": "./WatchedFiles/Invoices",
      "Filter": "*.json",
      "Options": {
        "Archive": "true",
        "ArchiveDirectory": "ProcessedInvoices",
        "RetryCount": "3",
        "RetryDelayMs": "2000",
        "ProcessExistingFiles": "true"
      }
    }
  },
  "JobScheduler": {
    "DailyReportJob": {
      "Enabled": true,
      "CronExpression": "0 0 8 * * ?",
      "Description": "Generate daily reports at 8 AM",
      "JobData": {
        "ReportType": "Daily",
        "OutputPath": "./Reports"
      }
    }
  },
  "DBPE": {
    "Messaging": {
      "UseRabbitMQ": false,
      "EnableRetries": true,
      "DefaultRetryCount": 3
    }
  }
}
```

## 🎯 Message Flow

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ Invoice File    │───→│ InvoiceFile      │───→│ Invoice         │
│ (JSON)          │    │ Watcher          │    │ Consumer        │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                ↓
                       ┌──────────────────┐
                       │ Message Bus      │
                       │ (Rebus)          │
                       └──────────────────┘
                                ↑
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ Report          │←───│ Daily Report     │←───│ Scheduled       │
│ Consumer        │    │ Job              │    │ (Every 5 min)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## 🏃‍♂️ Running the Example

### Prerequisites
- .NET 8.0 SDK installed
- Visual Studio 2022, VS Code, or JetBrains Rider

### Steps
1. **Clone and build:**
   ```bash
   git clone <repository>
   cd DBPE/DBPE.Example
   dotnet build
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **What you'll see:**
   - 🚀 Application startup logs
   - 📁 Directory creation for file watching
   - 📄 Sample invoice file created automatically
   - 📊 Invoice processing messages
   - ⏰ Scheduled job execution every 5 minutes
   - 🔄 Consumer processing logs
   - 🌐 Interactive webhook test page at `/test/webhooks`

## 📊 Sample Output

```
🚀 Starting DBPE Example Application
✅ DBPE Engine started successfully
📁 File watcher monitoring: ./WatchedFiles/Invoices/*.json
⏰ Daily report job scheduled to run every 5 minutes
🔄 Message consumers ready to process invoices and reports
📂 Demo directories created
📄 Sample invoice file created: ./WatchedFiles/Invoices/invoice-20241218-143022.json

📄 Processing Invoice Message
   Invoice ID: INV-2024-001
   Customer: Acme Corporation
   Amount: $1,250.00
   Invoice Date: 2024-12-16
   Source File: invoice-20241218-143022.json
   Items Count: 2
   📋 Invoice Items:
      • Software License - Qty: 5 @ $200.00 = $1,000.00
      • Support Services - Qty: 1 @ $250.00 = $250.00
   Processed At: 2024-12-18 14:30:25
✅ Invoice INV-2024-001 processing completed successfully

📊 Processing Report Generation Message
   Report ID: 550e8400-e29b-41d4-a716-446655440000
   Report Type: Daily
   Generated At: 2024-12-18 14:35:00
   Record Count: 127
   Total Amount: $32,847.50
   📝 Summary: Generated report with 127 records totaling $32,847.50
🔔 Simulated Actions:
   • Email notification sent to managers
   • Report archived to file system
   • Database updated with report metrics
   • Dashboard refreshed with new data
✅ Report processing completed successfully
```

## 🔧 Customization

### Adding New File Watchers
1. Create a class implementing `IFileWatcher`
2. Add `[FileWatcherConfig]` attribute
3. Configure in `appsettings.json`

### Adding New Jobs
1. Create a class implementing `IDbpeJob`
2. Add `[AutoSchedule]` and `[JobConfig]` attributes
3. Configure schedule in attributes or `appsettings.json`

### Adding New Consumers
1. Create a class implementing `IConsumer<T>` following DBPE conventions
2. Add `[ConsumerConfig]` attribute
3. The framework automatically discovers and registers consumers

### Adding New Webhooks
1. Create a class implementing `IWebHookHandler`
2. Add `[WebHookHandler]` attribute with path
3. Handle incoming requests and send messages

## 🔧 Dead Letter Queue (DLQ) Management API

The DBPE.Example application exposes a comprehensive REST API for managing failed messages, enabling easy monitoring and recovery of processing errors.

### 📍 API Endpoints Available

**Base URL:** `http://localhost:5000/api/dlq/`

#### **Query Failed Messages**
```bash
# Get all failed messages with pagination
curl "http://localhost:5000/api/dlq/messages"

# Filter by message type and reprocessable messages
curl "http://localhost:5000/api/dlq/messages?messageType=InvoiceProcessedContract&canReprocess=true"

# Search for specific errors
curl "http://localhost:5000/api/dlq/messages?searchText=InvoiceErrorConsumer"
```

#### **View Failed Message Details**
```bash
# Get specific failed message
curl "http://localhost:5000/api/dlq/messages/{messageId}"
```

#### **Get DLQ Statistics**
```bash
# View DLQ statistics and counts
curl "http://localhost:5000/api/dlq/statistics"
```

#### **Reprocess Failed Messages**
```bash
# Reprocess a single message
curl -X POST "http://localhost:5000/api/dlq/messages/{messageId}/reprocess"

# Bulk reprocess multiple messages
curl -X POST "http://localhost:5000/api/dlq/messages/bulk-reprocess" \
  -H "Content-Type: application/json" \
  -d '{"messageIds": ["guid1", "guid2"]}'
```

#### **Edit Message Payloads**
```bash
# Edit message payload before reprocessing
curl -X PUT "http://localhost:5000/api/dlq/messages/{messageId}" \
  -H "Content-Type: application/json" \
  -d '{"payload": "{\"InvoiceId\":\"INV-001\",\"Amount\":1500.00}"}'
```

#### **Delete Failed Messages**
```bash
# Delete single message
curl -X DELETE "http://localhost:5000/api/dlq/messages/{messageId}"

# Bulk delete multiple messages
curl -X DELETE "http://localhost:5000/api/dlq/messages/bulk-delete" \
  -H "Content-Type: application/json" \
  -d '{"messageIds": ["guid1", "guid2"]}'
```

### 🎯 Error Handling Modes

The example demonstrates both error handling modes:

**Simple Mode** (Current configuration: `VerboseFailureTracking: false`):
- Tracks only the final failure for each message
- Lightweight and high-performance
- Updates existing failures with latest error details

**Verbose Mode** (Set `VerboseFailureTracking: true`):
- Tracks every failure attempt separately
- Complete audit trail of all retry attempts
- Full historical failure tracking

### 📊 Failure Source Classification

The API distinguishes between two types of failures:

- **`"Consumer"`** - Original consumer failures (e.g., InvoiceProcessedConsumer failing)
- **`"ErrorConsumer"`** - Error consumer failures (e.g., InvoiceErrorConsumer failing)

### 🔍 Example API Response

```json
{
  "items": [
    {
      "id": "06f4cf6b-464d-4f85-9830-f007864337a6",
      "rebusMessageId": "4de587fe-1de7-4c45-9786-d50274744275",
      "messageType": "InvoiceProcessedContract",
      "failedAt": "2025-09-23T01:24:45.3634468",
      "errorType": "System.InvalidOperationException",
      "errorMessage": "TEST: Simulating invoice processing failure for INV-HIGH-VALUE-001",
      "retryCount": 5,
      "canReprocess": true,
      "failureSource": "Consumer",
      "originalConsumerType": null
    },
    {
      "id": "ef3129f7-bfc1-4076-8347-47f1d227c1d2",
      "rebusMessageId": "4de587fe-1de7-4c45-9786-d50274744275",
      "messageType": "InvoiceProcessedContract",
      "failedAt": "2025-09-23T01:24:45.2797785",
      "errorType": "System.Exception",
      "errorMessage": "Simulated error for testing purposes",
      "retryCount": 4,
      "canReprocess": true,
      "failureSource": "ErrorConsumer",
      "originalConsumerType": "InvoiceErrorConsumer"
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 50,
  "hasNextPage": false
}
```

### 🌐 Interactive API Documentation

Visit the Swagger UI for full interactive API documentation:

**Swagger URL:** `http://localhost:5000/swagger`

### 🎯 Common DLQ Management Tasks

**Monitor Error Consumer Failures:**
```bash
# Find all second-level error failures
curl "http://localhost:5000/api/dlq/messages" | jq '.items[] | select(.failureSource == "ErrorConsumer")'
```

**Bulk Reprocess Invoice Failures:**
```bash
# Get all failed invoice messages and reprocess them
FAILED_IDS=$(curl -s "http://localhost:5000/api/dlq/messages?messageType=InvoiceProcessedContract&canReprocess=true" | jq -r '.items[].id')

curl -X POST "http://localhost:5000/api/dlq/messages/bulk-reprocess" \
  -H "Content-Type: application/json" \
  -d "{\"messageIds\": $(echo $FAILED_IDS | jq -R 'split("\n") | map(select(. != ""))')}"
```

**Clean Up Old Failures:**
```bash
# Delete failures older than 7 days
curl "http://localhost:5000/api/dlq/messages?toDate=$(date -d '7 days ago' -I)" | \
  jq -r '.items[].id' | \
  xargs -I {} curl -X DELETE "http://localhost:5000/api/dlq/messages/{}"
```

### ⚙️ Configuration

DLQ Management is configured in `appsettings.json`:

```json
{
  "DBPE": {
    "Messaging": {
      "Persistence": {
        "DeadLetterQueue": {
          "Management": {
            "EnableApi": true,
            "ApiPrefix": "/api/dlq",
            "RequireAuthorization": false,
            "EnableBulkOperations": true
          },
          "Reprocessing": {
            "AllowEdit": true,
            "MaxReprocessAttempts": 3,
            "ValidateBeforeReprocess": true
          }
        }
      }
    }
  }
}
```

## 🏗️ Architecture Benefits

- **Modular Design:** Each component is independently testable
- **Event-Driven:** Loose coupling between components
- **Scalable:** Easy to add new processors and consumers
- **Configurable:** Behavior controlled via configuration
- **Reliable:** Built-in retry logic and error handling
- **Observable:** Comprehensive logging and monitoring

## 🎓 Learning Objectives

This example teaches:
- Modern .NET dependency injection patterns
- Event-driven architecture with message buses
- File system monitoring and processing
- Scheduled job execution with Quartz.NET
- Webhook handling and API integration
- Configuration-driven application behavior
- Structured logging and observability

## 🤝 Contributing

To extend this example:
1. Fork the repository
2. Create feature branches
3. Add tests for new functionality
4. Submit pull requests

## 📚 Related Documentation

- [DBPE.Core](../DBPE.Core/README.md) - Core engine and abstractions
- [DBPE.Messaging](../DBPE.Messaging/README.md) - Message bus integration
- [DBPE.FileWatcher](../DBPE.FileWatcher/README.md) - File monitoring
- [DBPE.JobScheduler](../DBPE.JobScheduler/README.md) - Job scheduling
- [DBPE.WebHooks](../DBPE.WebHooks/README.md) - Webhook handling
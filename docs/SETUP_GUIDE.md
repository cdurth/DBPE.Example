# DBPE Example Setup Guide

## Quick Setup with PostgreSQL Persistence

### Option 1: Using Setup Scripts (Recommended)

We provide setup scripts for different platforms to configure your Supabase PostgreSQL connection:

#### Windows (PowerShell)
```powershell
.\setup-postgres.ps1 -Password "your-supabase-password"
```

#### Windows (Command Prompt)
```cmd
setup-postgres.bat your-supabase-password
```

#### Linux/Mac
```bash
./setup-postgres.sh your-supabase-password
```

### Option 2: Manual Configuration

#### Using Environment Variables

**Windows (PowerShell):**
```powershell
$env:DBPE__Messaging__PostgresConnectionString="Host=db.lmrrkipnzrltopngvkqn.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password"
```

**Windows (Command Prompt):**
```cmd
set DBPE__Messaging__PostgresConnectionString=Host=db.lmrrkipnzrltopngvkqn.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password
```

**Linux/Mac:**
```bash
export DBPE__Messaging__PostgresConnectionString="Host=db.lmrrkipnzrltopngvkqn.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password"
```

#### Using User Secrets (Development Only)

```bash
dotnet user-secrets init
dotnet user-secrets set "DBPE:Messaging:PostgresConnectionString" "Host=db.lmrrkipnzrltopngvkqn.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-password"
```

#### Direct Configuration (Not Recommended)

⚠️ **Security Warning:** Only use this for testing. Never commit passwords to source control!

Edit `appsettings.json`:

```json
{
  "DBPE": {
    "Messaging": {
      "PostgresConnectionString": "Host=db.lmrrkipnzrltopngvkqn.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your-actual-password"
    }
  }
}
```

## Running the Application

Once configured, run the application:

```bash
dotnet run
```

## What to Expect

1. **Automatic Setup**: The application will create all necessary database tables on first run
2. **Message Persistence**: All messages are saved to PostgreSQL before processing
3. **Failure Recovery**: If the application crashes, messages will resume processing on restart
4. **Scheduled Messages**: Delayed messages are persisted and delivered at the scheduled time
5. **Monitoring**: Check your Supabase dashboard to see message queues in real-time

## Verification

### Check Database Connection
Look for these log messages on startup:
```
[Information] Configuring PostgreSQL persistence for messaging
[Information] Enabled durable local queues
[Information] Enabled durable outbox pattern
[Information] Message storage migration completed successfully
```

### Monitor Messages in Supabase

1. Go to your Supabase project dashboard
2. Navigate to **Table Editor**
3. Select the **messaging** schema
4. View tables:
   - `wolverine_incoming_envelopes` - Message queue
   - `wolverine_scheduled` - Scheduled messages
   - `wolverine_dead_letters` - Failed messages

### Test Persistence

1. Let the application send some test messages
2. Stop the application (Ctrl+C) while messages are processing
3. Check Supabase to see pending messages
4. Restart the application - processing resumes automatically!

## Troubleshooting

### Connection Failed
- Verify your password is correct
- Check if Supabase project is active
- Ensure no firewall is blocking the connection

### Permission Denied
- Verify the database user has necessary permissions
- Check if the messaging schema can be created

### No Messages Persisted
- Ensure `UsePostgresPersistence` is set to `true`
- Check for migration errors in logs
- Verify the connection string is loaded correctly

## Security Best Practices

1. **Never hardcode passwords** in source files
2. **Use environment variables** or user secrets
3. **Rotate passwords regularly**
4. **Use connection pooling** for production
5. **Monitor database connections** in Supabase dashboard

## Next Steps

- Explore the message persistence features
- Try stopping/starting the app during processing
- Monitor your messages in the Supabase dashboard
- Implement your own message handlers with persistence
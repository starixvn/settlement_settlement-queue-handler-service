# Quick Start Guide

## Prerequisites
- .NET 9.0 SDK installed
- Access to RabbitMQ server (configured in appsettings.json)

## Quick Start

### 1. Navigate to the project directory
```bash
cd D:\settlement-queue-handler-service\Tests\Settlement.TestPublisher
```

### 2. Run the application
```bash
dotnet run
```

### 3. Use the interactive menu

When the application starts, you'll see:

```
╔════════════════════════════════════════════════════════════╗
║     JackTime Hybrid Message Publisher Test Tool           ║
╚════════════════════════════════════════════════════════════╝

Choose an option:
1. Send single test message
2. Send multiple test messages
3. Send custom message
4. Exit

Enter your choice (1-4):
```

### Quick Test Examples

#### Send a Single Test Message
```
Enter your choice (1-4): 1

✅ Message published successfully!
   Queue: settlement-jacktime-hybrid
   Match ID: 12345
   User ID: 1001
   Log Date: 2025-10-30 14:30:00
```

#### Send 10 Test Messages
```
Enter your choice (1-4): 2
How many messages would you like to send? 10

Sending 10 messages...
Progress: 10/10
✅ Successfully sent 10 messages!
```

#### Send Custom Message
```
Enter your choice (1-4): 3
Enter Match ID: 99999
Enter User ID: 5000
Use current date/time? (Y/n): y

✅ Message published successfully!
   Queue: settlement-jacktime-hybrid
   Match ID: 99999
   User ID: 5000
   Log Date: 2025-10-30 14:30:00
```

## Configuration for Local Testing

To test with a local RabbitMQ instance:

### Windows PowerShell
```powershell
$env:ASPNETCORE_ENVIRONMENT="Local"
dotnet run
```

### Windows Command Prompt
```cmd
set ASPNETCORE_ENVIRONMENT=Local
dotnet run
```

### Linux/macOS
```bash
export ASPNETCORE_ENVIRONMENT=Local
dotnet run
```

## Verifying Messages

1. **RabbitMQ Management UI**: 
   - Open http://rabbitmq.nexdev.net:15672 (or your RabbitMQ management URL)
   - Navigate to Queues
   - Look for `settlement-jacktime-hybrid` queue
   - Check message count

2. **Consumer Logs**:
   - Monitor the Settlement.QueueHandlerService logs
   - Look for "JackTime Hybrid Consumer" log entries

3. **Database**:
   - Check relevant tables for processed match data
   - Verify settlement records were created

## Troubleshooting

### Cannot connect to RabbitMQ
```
❌ Failed to publish message: Cannot connect to RabbitMQ
```
**Solution**: 
- Check RabbitMQ is running
- Verify credentials in appsettings.json
- Test network connectivity: `ping rabbitmq.nexdev.net`

### Messages not being consumed
**Solution**:
- Ensure Settlement.QueueHandlerService is running
- Check consumer is configured for `settlement-jacktime-hybrid` queue
- Verify queue bindings in RabbitMQ

## Tips

- Start with option 1 (single message) for initial testing
- Use option 2 for load testing
- Use option 3 for specific test scenarios
- Keep the consumer service running before publishing messages
- Monitor both publisher and consumer logs simultaneously

## Support

For issues or questions, refer to the main README.md file.


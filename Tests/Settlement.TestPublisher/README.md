# Settlement Test Publisher

A console application for testing the JackTime Hybrid consumer by publishing messages to RabbitMQ via MassTransit.

## Overview

This tool allows you to easily publish `HybridMatch` messages to the `settlement-jacktime-hybrid` queue for testing purposes.

## Configuration

The RabbitMQ connection settings are configured in `appsettings.json`:

```json
{
  "RabbitMQ": {
    "Host": "rabbitmq.nexdev.net",
    "VirtualHost": "/settlement",
    "Username": "settlement",
    "Password": "Settle@0918"
  }
}
```

### Local Testing

For local testing with a local RabbitMQ instance, use the `appsettings.Local.json` configuration:

```bash
# Set environment to use local configuration
$env:ASPNETCORE_ENVIRONMENT="Local"
dotnet run
```

Or simply modify `appsettings.json` directly to point to your desired RabbitMQ instance.

## HybridMatch Message Structure

The publisher sends messages with the following structure:

```csharp
public record HybridMatch(int MatchId, DateTime LogDate, int UserId);
```

## Usage

### Running the Application

```bash
cd Tests\Settlement.TestPublisher
dotnet run
```

### Features

The tool provides four options:

#### 1. Send Single Test Message
Publishes a single pre-configured test message:
- Match ID: 12345
- User ID: 1001
- Log Date: Current date/time

#### 2. Send Multiple Test Messages
Allows you to specify the number of messages to send. Each message will have:
- Match ID: Sequential starting from 10000
- User ID: Varies between 1001-1010
- Log Date: Current date/time

Example output:
```
How many messages would you like to send? 10
Sending 10 messages...
Progress: 10/10
✅ Successfully sent 10 messages!
```

#### 3. Send Custom Message
Provides full control over message content:
- Enter custom Match ID
- Enter custom User ID
- Choose between current time or custom date/time

Example:
```
Enter Match ID: 99999
Enter User ID: 5000
Use current date/time? (Y/n): n
Enter date (yyyy-MM-dd HH:mm:ss): 2025-10-30 14:30:00

✅ Message published successfully!
   Queue: settlement-jacktime-hybrid
   Match ID: 99999
   User ID: 5000
   Log Date: 2025-10-30 14:30:00
```

#### 4. Exit
Closes the application

## Queue Information

- **Queue Name**: `settlement-jacktime-hybrid`
- **Consumer**: `JackTimeHybridConsumer`
- **Message Type**: `HybridMatch`

## Testing Workflow

1. Start the Settlement.QueueHandlerService consumer application
2. Run this test publisher
3. Send test messages using any of the three methods
4. Monitor the consumer logs to verify message processing
5. Check database/logs for expected results

## Troubleshooting

### Connection Issues
- Verify RabbitMQ is running and accessible
- Check credentials in `appsettings.json`
- Ensure the virtual host `/settlement` exists
- Verify network connectivity to the RabbitMQ server

### Message Not Being Consumed
- Ensure the consumer service is running
- Verify the queue name matches the consumer's endpoint
- Check RabbitMQ management UI for messages in the queue
- Review consumer logs for any errors

## Development

To modify the message structure or add new features:

1. Update the `HybridMatch` record in `Settlement.Consumer.Models.JackTimeHybrid`
2. Rebuild both projects
3. Update the message creation logic in `Program.cs`

## Dependencies

- MassTransit.RabbitMQ 8.5.2
- Microsoft.Extensions.Hosting 9.0.9
- Microsoft.Extensions.Configuration.Json 9.0.0
- Settlement.Consumer (project reference)


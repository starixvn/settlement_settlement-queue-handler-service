using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Settlement.Consumer.Constants;

namespace Settlement.TestPublisher;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     JackTime Hybrid Message Publisher                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        using var scope = host.Services.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        // Create the specific message with timezone-aware date
        var logDate = new DateTimeOffset(2025, 10, 30, 14, 42, 0, TimeSpan.FromHours(7));

        var message = new HybridMatch
        {
            Action = "CAL",
            MatchId = 83820770,
            LogDate = logDate.DateTime,
            UserId = 0
        };


        Console.WriteLine("Publishing message:");
        Console.WriteLine($"  MatchId: {message.MatchId}");
        Console.WriteLine($"  LogDate: {logDate:yyyy-MM-ddTHH:mm:sszzz}");
        Console.WriteLine($"  UserId: {message.UserId}");
        Console.WriteLine();

        await PublishMessage(publishEndpoint, message);

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                services.AddMassTransit(x =>
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        var rabbitHost = configuration["RabbitMQ:Host"];
                        var virtualHost = configuration["RabbitMQ:VirtualHost"];
                        var username = configuration["RabbitMQ:Username"];
                        var password = configuration["RabbitMQ:Password"];

                        cfg.Host(rabbitHost, virtualHost, h =>
                        {
                            h.Username(username ?? string.Empty);
                            h.Password(password ?? string.Empty);
                        });
                    });
                });
            });

    static async Task PublishMessage(IPublishEndpoint publishEndpoint, HybridMatch message)
    {
        try
        {
            await publishEndpoint.Publish(message, context =>
            {
                context.SetRoutingKey(ConsumeType.JackTimeHybrid);
            });

            Console.WriteLine("✅ Message published successfully!");
            Console.WriteLine($"   Queue: {ConsumeType.JackTimeHybrid}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Failed to publish message: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
    }
}

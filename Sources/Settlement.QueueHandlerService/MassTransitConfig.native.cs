using MassTransit;
using Settlement.Consumer.Consumers;

namespace Settlement.QueueHandlerService;

public static class MassTransitConfig
{
    public static void AddMassTransitWithRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitHost = configuration["RabbitMQ:Host"];
        var virtualHost = configuration["RabbitMQ:VirtualHost"];
        var endpoints = configuration.GetSection("RabbitMQ:Endpoints").Get<List<string>>();

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddConsumer<SoccerJackTimeConsumer, MyConsumerDefinition>();
            busConfigurator.AddConsumer<JackTimeHybridConsumer, JackTimeHybridConsumerDefinition>();

            busConfigurator.UsingRabbitMq((context, busFactoryConfigurator) =>
            {
                busFactoryConfigurator.Host(rabbitHost, virtualHost, h =>
                {
                    h.Username(configuration["RabbitMQ:Username"] ?? string.Empty);
                    h.Password(configuration["RabbitMQ:Password"] ?? string.Empty);
                });

                busFactoryConfigurator.ConfigureEndpoints(context);
            });
        });
    }
}
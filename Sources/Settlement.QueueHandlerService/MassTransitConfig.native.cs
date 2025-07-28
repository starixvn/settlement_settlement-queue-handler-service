using MassTransit;
using Settlement.Consumer.Constants;
using Settlement.Consumer.Consumers;

public static class MassTransitConfig
{
    public static void AddMassTransitWithRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitHost = configuration["RabbitMQ:Host"];
        var virtualHost = configuration["RabbitMQ:VirtualHost"];
        var endpoints = configuration.GetSection("RabbitMQ:Endpoints").Get<List<string>>();

        services.AddMassTransit(config =>
        {
            config.AddConsumer<SoccerJackTimeConsumer>();

            config.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitHost, virtualHost, h =>
                {
                    h.Username(configuration["RabbitMQ:Username"] ?? string.Empty);
                    h.Password(configuration["RabbitMQ:Password"] ?? string.Empty);
                });

                if (endpoints != null)
                {
                    foreach (var endpoint in endpoints)
                    {
                        cfg.ReceiveEndpoint(endpoint, e =>
                        {
                            switch (endpoint)
                            {
                                case ConsumeType.SoccerJackTime:
                                    e.ConfigureConsumer<SoccerJackTimeConsumer>(ctx);
                                    break;
                            }
                        });
                    }
                }
            });
        });
    }
}
using Fanex.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Settlement.Consumer;
using Settlement.Consumer.Services;
using Settlement.Shared;
using StackExchange.Redis;

namespace Settlement.QueueHandlerService;

public static class ServiceExtensions
{
    public static void AddAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<ConsumeMapperProfile>();
            cfg.AddProfile<MapperProfile>();
        });
    }

    public static void AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IRequestService, RequestService>();
        services.AddSingleton<ISoccerJackTimeService, SoccerJackTimeService>();
    }

    public static void AddRedisCacheConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfig = configuration.GetSection("Fanex.Caching.Redis:RedisConfig").Value;
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect($"{redisConfig}"));
    }
}
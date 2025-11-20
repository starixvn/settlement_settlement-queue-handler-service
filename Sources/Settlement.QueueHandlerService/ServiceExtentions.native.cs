using Fanex.Caching;
using Fanex.Caching.Redis;
using Settlement.Consumer;
using Settlement.Consumer.Services;
using Settlement.Shared;
using Settlement.Shared.Services;
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
        services.AddSingleton<HybridParlayService, HybridParlayServiceImp>();
        services.AddSingleton<ILetsTalkService, LetsTalkService>();
        services.AddSingleton<HybridParlayLogService, HybridParlayLogServiceImp>();
        services.AddSingleton<OddsService, OddsServiceImp>();
    }

    public static void AddRedisCacheConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("Fanex.Caching.Redis:ConnectionString");
        var applicationName = configuration.GetValue<string>("Fanex.Caching.Redis:ApplicationName");

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect($"{connectionString},ChannelPrefix={applicationName}"));
        services.AddSingleton<ICache>(new RedisCache(new RedisCacheOptions
        {
            ConnectionString = connectionString,
            ApplicationName = applicationName,
        }));
    }
}
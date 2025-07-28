using AutoMapper;
using Settlement.Consumer;
using Settlement.Consumer.Services;
using Settlement.Shared;

namespace Settlement.QueueHandlerService;

public static class ServiceExtensions
{
    public static void AddAutoMapper(this IServiceCollection services)
    {

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(typeof(ConsumeMapperProfile));
            mc.AddProfile(typeof(MapperProfile));
        });

        var mapper = mapperConfig.CreateMapper();
        services.AddSingleton(mapper);

    }

    public static void AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IRequestService, RequestService>();
        services.AddSingleton<ISoccerJackTimeService, SoccerJackTimeService>();
    }
}
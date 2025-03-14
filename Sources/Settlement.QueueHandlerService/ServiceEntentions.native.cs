using AutoMapper;
using Fanex.RemotingRef;
using Settlement.Consumer;
using Settlement.Consumer.Services;

namespace Settlement.QueueHandlerService;

public static class ServiceExtensions
{
    public static void AddAutoMapper(this IServiceCollection services)
    {

        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(typeof(ConsumeMapperProfile));
        });

        var mapper = mapperConfig.CreateMapper();
        services.AddSingleton(mapper);

    }

    public static void AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IInvokeExternalAPI, InvokeExternalAPI>();
        services.AddSingleton<IRequestService, RequestService>();
        services.AddSingleton<SendMailService, SendMailServiceImp>();
        services.AddSingleton<IHandleResponseService, HandleResponseService>();
        services.AddSingleton<IRemotingRefService, RemotingRefService>();
        services.AddSingleton<ISoccerJackTimeService, SoccerJackTimeService>();
    }
}
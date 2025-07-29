using Fanex.Data.Repository;
using Fanex.RemotingRef;
using Microsoft.Extensions.DependencyInjection;
using Settlement.Consumer.Services;
using Settlement.Shared.Services;

namespace Settlement.Shared;

public static class ServiceExtension
{
    public static void AddSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IInvokeExternalAPI, InvokeExternalAPI>();
        services.AddSingleton<SendMailService, SendMailServiceImp>();
        services.AddSingleton<IHandleResponseService, HandleResponseService>();
        services.AddSingleton<IRemotingRefService, RemotingRefService>();
        services.AddSingleton<IDynamicRepository, DynamicRepository>();
    }
}
using Microsoft.Extensions.DependencyInjection;
using Settlement.JackTime.Services;

namespace Settlement.JackTime;

public static class JackTimeExtensions
{
    public static IServiceCollection AddJackTimeServices(this IServiceCollection services)
    {
        services.AddSingleton<ISoccerJackTimeService, SoccerJackTimeService>();
        services.AddSingleton<IRequestService, RequestService>();
        services.AddSingleton<IJackTimeService, JackTimeService>();
        services.AddSingleton<IJackTimeController, JackTimeController>();

        return services;
    }
}
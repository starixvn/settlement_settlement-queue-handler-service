using Microsoft.Extensions.DependencyInjection;
using Settlement.JackTime.Services;
using Settlement.JackTime.Services.Hybrid;

namespace Settlement.JackTime;

public static class JackTimeExtensions
{
    public static IServiceCollection AddJackTimeServices(this IServiceCollection services)
    {
        services.AddSingleton<ISoccerJackTimeService, SoccerJackTimeService>();
        services.AddSingleton<IRequestService, RequestService>();
        services.AddSingleton<IJackTimeService, JackTimeService>();
        services.AddSingleton<IJackTimeController, JackTimeController>();
        services.AddSingleton<IGetNotificationService, GetNotificationService>();
        services.AddSingleton<INotificationProcessing, NotificationProcessing>();

        return services;
    }
}
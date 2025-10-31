using Microsoft.Extensions.Options;
using Settlement.JackTime.Models;
using Settlement.Shared.Models;
using Settlement.Shared.Services;

namespace Settlement.JackTime.Services.Hybrid;

public interface IGetNotificationService
{
    Task<Notification> GetNotifications(long offSet);
}

public class GetNotificationService(
    IInvokeExternalAPI invokeExternalApi,
    IOptions<AppSettingsKey> options
    ) : IGetNotificationService
{
    public async Task<Notification> GetNotifications(long offSet)
    {
        var jackTimeHybridUrl = options.Value.InternalApiURL + "jacktime-cash-out/notifications";
        return await callExternalAPI(jackTimeHybridUrl, offSet);
    }

    private async Task<Notification> callExternalAPI(string jackTimeHybridUrl, long offset)
    {
        var notificationResponse = await invokeExternalApi.GetAsync<NotificationResponse>(jackTimeHybridUrl + $"/{offset}");

        return notificationResponse?.Data;
    }
}
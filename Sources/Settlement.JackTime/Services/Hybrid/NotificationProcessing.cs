using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Settlement.JackTime.Criteria;
using Settlement.JackTime.Models;
using Settlement.Shared.Models;

namespace Settlement.JackTime.Services.Hybrid;

public interface INotificationProcessing
{
    Task ProcessNotifications();
}

public class NotificationProcessing(
    IGetNotificationService getNotificationService,
    IDynamicRepository repository,
    IOptions<AppSettingsKey> options) : INotificationProcessing
{
    public async Task ProcessNotifications()
    {
        var offSetCriteria = new GetOffsetCriteria
        {
            Environment = options.Value.EnvironmentName
        };

        await repository.ExecuteAsync(offSetCriteria);
        var notifications = await getNotificationService.GetNotifications(offSetCriteria.MaxOffSet);
        if (notifications != null)
        {
            await InsertHybridParlayLog(notifications);
        }
    }

    private async Task InsertHybridParlayLog(Notification notification)
    {
        var criteria = new InsertHybridParlayLogCriteria
        {
            LegOddsJSON = JsonConvert.SerializeObject(notification.Notifications),
            OffSet = notification.OffSet,
            Environment = options.Value.EnvironmentName
        };
        await repository.ExecuteAsync(criteria);
    }
}
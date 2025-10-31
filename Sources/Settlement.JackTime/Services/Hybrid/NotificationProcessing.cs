using Newtonsoft.Json;
using Settlement.JackTime.Models;

namespace Settlement.JackTime.Services.Hybrid;

public interface INotificationProcessing
{
    Task ProcessNotifications();
}

public class NotificationProcessing(IGetNotificationService getNotificationService, IDynamicRepository repository) : INotificationProcessing
{
    public async Task ProcessNotifications()
    {
        var offSet = await repository.GetAsync<int>(new GetOffsetCriteria());
        var notifications = await getNotificationService.GetNotifications(offSet);

        await insertHybridParlayLog(notifications);
    }

    private async Task insertHybridParlayLog(Notification notification)
    {

        var criteria = new InsertHybridParlayLogCriteria
        {
            LegOddsJSON = JsonConvert.SerializeObject(notification.Notifications),
            OffSet = notification.offSet
        };
        await repository.ExecuteAsync(criteria);
    }
}
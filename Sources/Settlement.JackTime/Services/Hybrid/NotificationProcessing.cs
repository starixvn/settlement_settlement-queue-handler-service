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
        var offSetCriteria = new GetOffsetCriteria();
        await repository.ExecuteAsync(offSetCriteria);
        Console.WriteLine($"Current Offset: {offSetCriteria.MaxOffSet}");
        var notifications = await getNotificationService.GetNotifications(offSetCriteria.MaxOffSet);
        Console.WriteLine($"End Notifi: {notifications}");
        await InsertHybridParlayLog(notifications);
    }

    private async Task InsertHybridParlayLog(Notification notification)
    {
        var criteria = new InsertHybridParlayLogCriteria
        {
            LegOddsJSON = JsonConvert.SerializeObject(notification.Notifications),
            OffSet = notification.offSet
        };
        await repository.ExecuteAsync(criteria);
    }
}
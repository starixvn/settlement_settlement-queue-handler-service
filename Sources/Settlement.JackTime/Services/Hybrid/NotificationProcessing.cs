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
        // var offset = await repository.GetAsync<int>(new GetOffsetCriteria());
        var offSet = 1;
        var notifications = await getNotificationService.GetNotifications(offSet);

        await insertHybridParlayLog(notifications, offSet);
    }

    private async Task insertHybridParlayLog(IEnumerable<Notification> notifications, long offSet)
    {

        var criteria = new InsertHybridParlayLogCriteria
        {
            LegOddsJSON = JsonConvert.SerializeObject(notifications),
            OffSet = offSet
        };
        await repository.ExecuteAsync(criteria);
    }
}
using ONEbook.UM.Services;
using Settlement.JackTime.Services.Hybrid;

namespace Settlement.QueueHandlerService.Services;

public class GetNotificationService(
    IConfiguration configuration,
    IMaintenanceService maintenanceService,
    ILogger<GetNotificationService> logger,
    INotificationProcessing notificationProcessing)
    : BaseSettlementService(configuration, maintenanceService, logger)
{
    protected override async Task ProcessAsync()
    {
        await notificationProcessing.ProcessNotifications();
    }

    protected override string ServiceIntervalConfigKey => "NotificationServiceInterval";

    protected override string ServiceName => "Settlement.GetNotification";
}
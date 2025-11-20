using ONEbook.UM.Services;
using Settlement.JackTime.Services;

namespace Settlement.QueueHandlerService.Services;

public class AutoJackTimeService(
    IJackTimeController jackTimeController,
    IConfiguration configuration,
    IMaintenanceService maintenanceService,
    ILogger<AutoJackTimeService> logger)
    : BaseSettlementService(configuration, maintenanceService, logger)
{
    protected override async Task ProcessAsync()
    {
        await jackTimeController.ProcessAsync();
    }

    protected override string ServiceIntervalConfigKey => "ServiceInterval";

    protected override int DefaultServiceIntervalMs => 5000;

    protected override string ServiceName => "Settlement.AutoJackTime";
}
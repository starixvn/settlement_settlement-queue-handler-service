using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ONEbook.UM.Services;
using Settlement.JackTime.Services;

namespace Settlement.QueueHandlerService.Services;

public class AutoJackTimeService(
    IJackTimeController jackTimeController,
    IConfiguration configuration,
    IMaintenanceService maintenanceService)
    : BackgroundService
{
    private int counter;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(configuration.GetValue<int>("ServiceInterval")));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await HandleAutoAsync();
        }
    }

    private async Task HandleAutoAsync()
    {
        var isMaintenance = await maintenanceService.IsUnderMaintenanceAsync();
        if (!isMaintenance)
        {
            await jackTimeController.ProcessAsync();
            HandleGarbageCollector();
        }
    }

    private void HandleGarbageCollector()
    {
        if (counter >= configuration.GetValue<int>("GarbageCollectorInterval"))
        {
            counter = 0;
            GC.WaitForPendingFinalizers();
        }

        counter += configuration.GetValue<int>("ServiceInterval");
    }
}
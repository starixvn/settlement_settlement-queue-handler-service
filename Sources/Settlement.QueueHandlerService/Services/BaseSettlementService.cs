using ONEbook.UM.Services;

namespace Settlement.QueueHandlerService.Services;

public abstract class BaseSettlementService(
    IConfiguration configuration,
    IMaintenanceService maintenanceService,
    ILogger logger) : BackgroundService
{
    protected readonly IConfiguration Configuration = configuration;
    protected readonly IMaintenanceService MaintenanceService = maintenanceService;
    protected readonly ILogger Logger = logger;

    private int counter;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMs = Configuration.GetValue<int>(ServiceIntervalConfigKey, DefaultServiceIntervalMs);
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(intervalMs));

        Logger.LogInformation("{ServiceName} background service started.", ServiceName);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await HandleProcessingAsync();
        }
    }

    private async Task HandleProcessingAsync()
    {
        try
        {
            var isMaintenance = await MaintenanceService.IsUnderMaintenanceAsync();
            if (!isMaintenance)
            {
                await ProcessAsync();
                HandleGarbageCollector();
            }
            else
            {
                Logger.LogDebug("System under maintenance, Stop processing.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while processing in {ServiceName}", ServiceName);
        }
    }

    private void HandleGarbageCollector()
    {
        var garbageCollectorInterval = Configuration.GetValue<int>("GarbageCollectorInterval", 300000);
        var serviceInterval = Configuration.GetValue<int>(ServiceIntervalConfigKey, DefaultServiceIntervalMs);

        if (counter >= garbageCollectorInterval)
        {
            counter = 0;
            GC.WaitForPendingFinalizers();
            Logger.LogDebug("Garbage collection executed for {ServiceName}.", ServiceName);
        }

        counter += serviceInterval;
    }

    protected abstract Task ProcessAsync();

    protected abstract string ServiceIntervalConfigKey { get; }

    protected virtual int DefaultServiceIntervalMs => 30000;

    protected abstract string ServiceName { get; }
}

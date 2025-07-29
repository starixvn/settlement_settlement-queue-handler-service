using Fanex.Logging;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.Database;

namespace Settlement.JackTime.Services;

public interface IJackTimeController
{
    Task ProcessAsync();
}

public class JackTimeController(IJackTimeService jackTimeService) : IJackTimeController
{
    public async Task ProcessAsync()
    {
        try
        {
            var newMatches = await jackTimeService.GetMatches();

            if (newMatches.Count == 0)
            {
                return;
            }

            var tasks = newMatches.Select(jackTimeService.Consume);
            await Task.WhenAll(tasks);
        }
        catch (Exception exception)
        {
            var logMessage = new LogMessage(exception.Message)
                .AddSection("Database Info", DatabaseInfoGetters.GetDbExceptionInfo(exception))
                .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
                .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
                .ToString();
            await LogService.ErrorAsync(logMessage);
        }
    }
}
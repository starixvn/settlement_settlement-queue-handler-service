namespace Settlement.JackTime.Services;


public interface IJackTimeController
{
    Task ProcessAsync();
}

public class JackTimeController(IJackTimeService jackTimeService) : IJackTimeController
{
    public async Task ProcessAsync()
    {
        var newMatches = await jackTimeService.GetMatches();
        newMatches.ForEach(m =>
        {
            Task.Run(() => jackTimeService.Consume(m))
                .ConfigureAwait(false);
        });
    }
}

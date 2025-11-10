using Fanex.Data.Repository;
using Settlement.Consumer.Criteria;
using Settlement.Shared.Services;

namespace Settlement.Consumer.Services;

public interface IHybridLogService
{
    Task NotExistLegOdds(int matchId, int LogId, IEnumerable<string> transIds);

    Task InvalidLegOdds(int matchId, int LogId, IEnumerable<string> transIds);

    Task<int> WriteLog(int matchId);
}

public class HybridLogService(ILetsTalkService letsTalkService, IDynamicRepository repository) : IHybridLogService
{
    private readonly int HybridLogId = 90433;

    public async Task InvalidLegOdds(int matchId, int LogId, IEnumerable<string> transIds)
    {
        var limitedTransIds = string.Join(", ", transIds.Take(5));
        var message = $"{LogId}  - Failed: Cannot settled due to invalid Leg Odds(Leg Odds > Placed Leg Odds).</br>TransIds: {limitedTransIds}";
        await letsTalkService.SendMessage(matchId, 1, "Get new Leg Odds", message);
    }

    public async Task NotExistLegOdds(int matchId, int LogId, IEnumerable<string> transIds)
    {
        var limitedTransIds = string.Join(", ", transIds.Take(5));
        var message = $"{LogId} - Failed: Cannot get new Leg odds, so some of the following TransIds might not be settled.</br>TransIds: {limitedTransIds}";
        await letsTalkService.SendMessage(matchId, 1, "Get new Leg Odds", message);
    }

    public async Task<int> WriteLog(int matchId)
    {
        var message = "No Odds Found";
        var criteria = new LogCriteria(matchId, message, HybridLogId);
        await repository.ExecuteAsync(criteria);
        return criteria.ActionId;
    }
}
using Fanex.Settlement.MessageBridge.JackTime;
using Settlement.Consumer.Criteria;
using Settlement.Shared.Services;

namespace Settlement.Consumer.Services;

public interface HybridParlayLogService
{
    Task InvalidTransId(HybridMatch hybridMatch, List<string> transIds);

    Task InvalidLegOdds(HybridMatch hybridMatch, List<string> transIds);
}

public class HybridParlayLogServiceImp(ILetsTalkService letsTalkService, IDynamicRepository repository) : HybridParlayLogService
{
    private readonly int HybridLogId = 90433;

    private const string ActionName = "Get new Leg Odds";

    public async Task InvalidTransId(HybridMatch hybridMatch, List<string> transIds)
    {
        if (transIds.Count > 0)
        {
            await WriteLog(hybridMatch.MatchId);
            var message = $"Failed: Cannot get new Leg odds, so some of the following TransIds might not be settled.{Environment.NewLine}TransIds: {string.Join(", ", transIds)}";
            await letsTalkService.SendMessage(hybridMatch.MatchId, hybridMatch.UserId, ActionName, message);
        }
    }

    private async Task WriteLog(int matchId)
    {
        const string message = "No Odds Found";
        var criteria = new LogCriteria(matchId, message, HybridLogId);
        await repository.ExecuteAsync(criteria);
    }

    public async Task InvalidLegOdds(HybridMatch hybridMatch, List<string> transIds)
    {
        var message = $"Failed: Cannot settled due to invalid Leg Odds(Leg Odds > Placed Leg Odds).{Environment.NewLine}TransIds: {string.Join(", ", transIds)}";
        await letsTalkService.SendMessage(hybridMatch.MatchId, hybridMatch.UserId, ActionName, message);
    }
}
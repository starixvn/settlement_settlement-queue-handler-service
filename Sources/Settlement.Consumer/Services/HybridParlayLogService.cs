using Fanex.Settlement.MessageBridge.JackTime;
using Settlement.Consumer.Criteria;
using Settlement.JackTime.Models;
using Settlement.Shared.Constants;
using Settlement.Shared.Services;

namespace Settlement.Consumer.Services;

public interface HybridParlayLogService
{
    Task InvalidTransIdsWriteLogAndSendLetstalk(HybridMatch hybridMatch, List<string> transIds);

    Task InvalidTransIdsSendLetstalk(HybridMatch hybridMatch, List<ValidationTransIds> validationTransIds);
}

public class HybridParlayLogServiceImp(ILetsTalkService letsTalkService, IDynamicRepository repository) : HybridParlayLogService
{
    private readonly int HybridLogId = 81067;

    private const string ActionName = "Get new Leg Odds";

    public async Task InvalidTransIdsWriteLogAndSendLetstalk(HybridMatch hybridMatch, List<string> transIds)
    {
        if (transIds.Count > 0)
        {
            var message = $"Failed: Cannot get new leg odds, some of the following TransIds might not be settled.{Environment.NewLine}TransIds: {string.Join(", ", transIds)}";
            await WriteLog(hybridMatch.MatchId, message);
            await letsTalkService.SendMessage(hybridMatch.MatchId, hybridMatch.UserId, ActionName, message, MessageGroup.HybridParlay);
        }
    }

    public async Task InvalidTransIdsSendLetstalk(HybridMatch hybridMatch, List<ValidationTransIds> validationTransIds)
    {
        var wrongOddsTransIds = ExtractTransIds(validationTransIds, v => v.WrongOddsTransIds);
        if (!string.IsNullOrEmpty(wrongOddsTransIds))
        {
            await SendInvalidLegOddsMessage(hybridMatch, wrongOddsTransIds);
        }

        var invalidTransIds = ExtractTransIds(validationTransIds, v => v.InvalidTransIds);
        if (!string.IsNullOrEmpty(invalidTransIds))
        {
            await SendInvalidTransIdsMessage(hybridMatch, invalidTransIds);
        }
    }

    private static string ExtractTransIds(
        List<ValidationTransIds> validationTransIds,
        Func<ValidationTransIds, string> propertySelector)
    {
        var transIds = validationTransIds
            .Where(v => !string.IsNullOrEmpty(propertySelector(v)))
            .SelectMany(v => propertySelector(v).Split(',').Select(t => t.Trim()))
            .Distinct();

        return string.Join(", ", transIds);
    }

    private async Task SendInvalidLegOddsMessage(HybridMatch hybridMatch, string transIds)
    {
        var message = $"Failed: Cannot settle due to invalid leg odds (leg odds > placed leg odds).{Environment.NewLine}TransIds: {transIds}";
        await WriteLog(hybridMatch.MatchId, message);
        await letsTalkService.SendMessage(hybridMatch.MatchId, hybridMatch.UserId, ActionName, message, MessageGroup.HybridParlay);
    }

    private async Task SendInvalidTransIdsMessage(HybridMatch hybridMatch, string transIds)
    {
        var message = $"Failed: Cannot update new leg odds, some of the following TransIds might not be settled.{Environment.NewLine}TransIds: {transIds}";
        await WriteLog(hybridMatch.MatchId, message);
        await letsTalkService.SendMessage(hybridMatch.MatchId, hybridMatch.UserId, ActionName, message, MessageGroup.HybridParlay);
    }

    private async Task WriteLog(int matchId, string message)
    {
        var criteria = new LogCriteria(matchId, message, HybridLogId);
        await repository.ExecuteAsync(criteria);
    }
}
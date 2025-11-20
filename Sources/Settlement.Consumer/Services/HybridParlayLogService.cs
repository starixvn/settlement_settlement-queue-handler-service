using Fanex.Settlement.MessageBridge.JackTime;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.JackTimeHybrid;
using Settlement.JackTime.Models;
using Settlement.Shared.Constants;
using Settlement.Shared.Services;

namespace Settlement.Consumer.Services;

public interface HybridParlayLogService
{
    Task InvalidTransIdsFetchNewOdds(HybridMatch hybridMatch, List<string> transIds, List<TranInfo> originalTransId);

    Task InvalidTransIdsUpdateOdds(HybridMatch hybridMatch, List<ValidationTransIds> validationTransIds, List<TranInfo> originalTransId);
}

public class HybridParlayLogServiceImp(ILetsTalkService letsTalkService, IDynamicRepository repository) : HybridParlayLogService
{
    private readonly int HybridLogId = 81067;

    private const string ActionName = "Get new Leg Odds";

    public async Task InvalidTransIdsFetchNewOdds(HybridMatch hybridMatch, List<string> transIds, List<TranInfo> originalTransId)
    {
        if (transIds.Count > 0)
        {
            var filteredOriginalTransId = originalTransId.Where(t => transIds.Contains(t.TransId)).ToList();
            var contentMessage = $"Failed: Cannot get new leg odds, some of the following TransIds might not be settled.{Environment.NewLine}";
            var message = BuildTransIdsMessage(filteredOriginalTransId, contentMessage);
            await WriteLog(hybridMatch.MatchId, message);
            await letsTalkService.SendMessage(hybridMatch.MatchId, hybridMatch.UserId, ActionName, message, MessageGroup.HybridParlay);
        }
    }

    private static string BuildTransIdsMessage(List<TranInfo> filteredListTransId, string contentMessage)
    {
        var licenseeTransIds = filteredListTransId.Where(t => !t.IsCredit).Select(t => t.TransId).ToList();
        var creditTransIds = filteredListTransId.Where(t => t.IsCredit).Select(t => t.TransId).ToList();

        var messageParts = new List<string> { contentMessage };

        if (licenseeTransIds.Count > 0)
        {
            messageParts.Add($"- Licensee TransId: {string.Join(", ", licenseeTransIds)}{Environment.NewLine}");
        }

        if (creditTransIds.Count > 0)
        {
            messageParts.Add($"- Credit TransId: {string.Join(", ", creditTransIds)}");
        }

        return string.Join("", messageParts);
    }

    public async Task InvalidTransIdsUpdateOdds(HybridMatch hybridMatch, List<ValidationTransIds> validationTransIds, List<TranInfo> originalTransId)
    {
        var wrongOddsTransIdsList = ExtractTransIdsList(validationTransIds, v => v.WrongOddsTransIds);
        if (wrongOddsTransIdsList.Count > 0)
        {
            await SendInvalidLegOddsMessage(hybridMatch, wrongOddsTransIdsList, originalTransId);
        }

        var invalidTransIdsList = ExtractTransIdsList(validationTransIds, v => v.InvalidTransIds);
        if (invalidTransIdsList.Count > 0)
        {
            await SendInvalidTransIdsMessage(hybridMatch, invalidTransIdsList, originalTransId);
        }
    }

    private static List<string> ExtractTransIdsList(
        List<ValidationTransIds> validationTransIds,
        Func<ValidationTransIds, string> propertySelector)
    {
        var transIds = validationTransIds
            .Where(v => !string.IsNullOrEmpty(propertySelector(v)))
            .SelectMany(v => propertySelector(v).Split(',').Select(t => t.Trim()))
            .Distinct()
            .ToList();

        return transIds;
    }

    private async Task SendInvalidLegOddsMessage(HybridMatch hybridMatch, List<string> transIdsList, List<TranInfo> originalTransId)
    {
        var filteredOriginalTransId = originalTransId.Where(t => transIdsList.Contains(t.TransId)).ToList();
        var contentMessage = $"Failed: Cannot settle due to invalid leg odds (leg odds > placed leg odds).{Environment.NewLine}";
        var message = BuildTransIdsMessage(filteredOriginalTransId, contentMessage);
        await WriteLog(hybridMatch.MatchId, message);
        await letsTalkService.SendMessage(hybridMatch.MatchId, hybridMatch.UserId, ActionName, message, MessageGroup.HybridParlay);
    }

    private async Task SendInvalidTransIdsMessage(HybridMatch hybridMatch, List<string> transIdsList, List<TranInfo> originalTransId)
    {
        var filteredOriginalTransId = originalTransId.Where(t => transIdsList.Contains(t.TransId)).ToList();
        var contentMessage = $"Failed: Cannot update new leg odds, some of the following TransIds might not be settled.{Environment.NewLine}";
        var message = BuildTransIdsMessage(filteredOriginalTransId, contentMessage);
        await WriteLog(hybridMatch.MatchId, message);
        await letsTalkService.SendMessage(hybridMatch.MatchId, hybridMatch.UserId, ActionName, message, MessageGroup.HybridParlay);
    }

    private async Task WriteLog(int matchId, string message)
    {
        var criteria = new LogCriteria(matchId, message, HybridLogId);
        await repository.ExecuteAsync(criteria);
    }
}
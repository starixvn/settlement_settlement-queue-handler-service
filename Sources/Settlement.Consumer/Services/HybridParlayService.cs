using Fanex.Settlement.MessageBridge.JackTime;
using Fanex.SiteDbConnectionManager.Client.Services;
using Polly;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.JackTimeHybrid;
using TaskResult = (System.Collections.Generic.List<Settlement.Consumer.Models.JackTimeHybrid.OddModel> Odds, System.Collections.Generic.List<string> InvalidTransId);

namespace Settlement.Consumer.Services;

public interface HybridParlayService
{
    Task<TaskResult> GetOddsAndInvalidTransId(int matchId, DateTimeOffset logDate, string transIds);

    Task<string> GetHyBridTransId(int matchId);

    Task UpdateOddsAndSettle(HybridMatch hybridMatch, IEnumerable<OddModel> odds);
}

public class HybridParlayServiceImp(
    IDynamicRepository repository,
    SiteDbClientService siteDbClientService,
    OddsService oddsService,
    HybridParlayLogService hybridParlayLogService) : HybridParlayService
{
    private const int RetryCount = 20;

    private const int RetryDelayMilliseconds = 3000;

    public async Task<TaskResult> GetOddsAndInvalidTransId(int matchId, DateTimeOffset logDate, string transIds)
    {
        var oddsResults = await oddsService.GetOdds(matchId, logDate, transIds);

        var inputTransIds = transIds.Split(',').Select(t => t.Trim()).ToList();
        var resultTransIds = new HashSet<string>(oddsResults.Select(r => r.TransId.ToString()));

        var invalidTransIds = inputTransIds.Except(resultTransIds).ToList();

        return (oddsResults, invalidTransIds);
    }

    public async Task<string> GetHyBridTransId(int matchId)
    {
        var siteDbServer = await siteDbClientService.GetActiveSiteDbAsync();
        var tasks = siteDbServer
            .Select(server => FetchTranInfoWithRetry(matchId, server.SiteName))
            .ToList();

        var results = await Task.WhenAll(tasks);
        var listTransIds = results.SelectMany(r => r);
        return string.Join(",", listTransIds.Select(t => t.TransId));
    }

    private async Task<IEnumerable<TranInfo>> FetchTranInfoWithRetry(int matchId, string siteName)
    {
        var retryPolicy = Policy
            .HandleResult<bool>(result => result)
            .WaitAndRetryAsync(
                RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(RetryDelayMilliseconds));

        var validateCommand = new HyBridValidateCriteria
        {
            MatchId = matchId,
            DbServer = siteName
        };

        IEnumerable<TranInfo> results = null;

        await retryPolicy.ExecuteAsync(async () =>
        {
            results = await repository.FetchAsync<TranInfo>(validateCommand);

            return validateCommand.ErrCode == 100;
        });

        return results;
    }

    public async Task UpdateOddsAndSettle(HybridMatch hybridMatch, IEnumerable<OddModel> odds)
    {
        if (odds.Any())
        {
            var siteDbServer = await siteDbClientService.GetActiveSiteDbAsync();
            var tasks = siteDbServer.Select(s => UpdateOddsAndSettleBySiteDb(hybridMatch, odds, s.SiteName));

            var results = await Task.WhenAll(tasks);
            var listTransIds = results.SelectMany(r => r).Distinct().ToList();

            if (listTransIds.Count > 0)
            {
                await hybridParlayLogService.InvalidLegOdds(hybridMatch, listTransIds);
            }
        }
    }

    private async Task<List<string>> UpdateOddsAndSettleBySiteDb(HybridMatch hybridMatch, IEnumerable<OddModel> odds, string siteName)
    {
        var (result, affectedTransIds) = await oddsService.UpdateOdds(odds, siteName, hybridMatch.UserId, hybridMatch.MatchId);
        
        if (!result.IsSuccessful)
        {
            return [.. odds.Select(o => o.TransId.ToString())];
        }

        if (string.IsNullOrEmpty(affectedTransIds))
        {
            return [.. odds.Select(o => o.TransId.ToString())];
        }

        await CalculateHybridMatch(hybridMatch, siteName, affectedTransIds);

        var invalidTransIds = GetTransIdsNotInAffected(odds, affectedTransIds);
        return invalidTransIds;
    }

    private static List<string> GetTransIdsNotInAffected(IEnumerable<OddModel> odds, string affectedTransIds)
    {
        var allTransIds = odds.Select(o => o.TransId.ToString()).ToList();

        var affectedTransIdsSet = new HashSet<string>(affectedTransIds.Split(',').Select(t => t.Trim()));
        return [.. allTransIds.Except(affectedTransIdsSet)];
    }

    private async Task CalculateHybridMatch(HybridMatch hybridMatch, string siteName, string transIds)
    {
        var criteria = new CalculateHybridMatchCriteria
        {
            MatchId = hybridMatch.MatchId,
            Creator = hybridMatch.UserId,
            Action = hybridMatch.Action,
            TransIds = transIds,
            DbServer = siteName
        };

        await repository.ExecuteAsync(criteria);
    }
}
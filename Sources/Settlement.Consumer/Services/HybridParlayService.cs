using Fanex.Settlement.MessageBridge.JackTime;
using Fanex.SiteDbConnectionManager.Client.Services;
using Polly;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.JackTimeHybrid;
using Settlement.JackTime.Models;
using TaskResult = (System.Collections.Generic.List<Settlement.Consumer.Models.JackTimeHybrid.OddModel> Odds, System.Collections.Generic.List<string> InvalidTransId);

namespace Settlement.Consumer.Services;

public interface HybridParlayService
{
    Task<TaskResult> GetOddsAndInvalidTransId(int matchId, DateTimeOffset logDate, string transIds);

    Task<string> GetHyBridTransId(int matchId, string action);

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

    public async Task<string> GetHyBridTransId(int matchId, string action)
    {
        var siteDbServer = await siteDbClientService.GetActiveSiteDbAsync();
        var tasks = siteDbServer
            .Select(server => FetchTranInfoWithRetry(matchId, action, server.SiteName))
            .ToList();

        var results = await Task.WhenAll(tasks);
        var listTransIds = results.SelectMany(r => r);
        return string.Join(",", listTransIds.Select(t => t.TransId));
    }

    private async Task<IEnumerable<TranInfo>> FetchTranInfoWithRetry(int matchId, string action, string siteName)
    {
        var retryPolicy = Policy
            .HandleResult<bool>(result => result)
            .WaitAndRetryAsync(
                RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(RetryDelayMilliseconds));

        var validateCommand = new HyBridValidateCriteria
        {
            MatchId = matchId,
            Action = action,
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
            var listTransIds = results.ToList();

            var hasInvalidTransIds = listTransIds.Exists(v =>
                !string.IsNullOrEmpty(v.WrongOddsTransIds) ||
                !string.IsNullOrEmpty(v.InvalidTransIds));

            if (hasInvalidTransIds)
            {
                await hybridParlayLogService.InvalidTransIdsSendLetstalk(hybridMatch, listTransIds);
            }
        }
    }

    private async Task<ValidationTransIds> UpdateOddsAndSettleBySiteDb(HybridMatch hybridMatch, IEnumerable<OddModel> odds, string siteName)
    {
        var results = await oddsService.UpdateOdds(odds, siteName, hybridMatch.UserId, hybridMatch.MatchId);

        if (!string.IsNullOrEmpty(results.AffectedTransIds))
        {
            await CalculateHybridMatch(hybridMatch, siteName, results.AffectedTransIds);
        }

        return results;
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
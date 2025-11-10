using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using Fanex.SiteDbConnectionManager.Client.Services;
using Newtonsoft.Json;
using Polly;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.JackTimeHybrid;
using Settlement.JackTime.Criteria;

namespace Settlement.Consumer.Services;

public interface IJackTimeHybridService
{
    Task ProcessHybridMatch(HybridMatch hybridMatch, IEnumerable<OddModel> odds, bool isUpdateOddSuccessfully);

    Task<(List<OddModel> odds, List<string> transIdsInvalid)> GetOdds(int matchId, DateTimeOffset logDate, string transIds);

    Task<bool> UpdateLegOdds(IEnumerable<OddModel> odds, int userId, int matchId);

    Task<string> GetHyBridTransId(int matchId);
}

public class JackTimeHybridService(
    IDynamicRepository repository,
    SiteDbClientService siteDbClientService) : IJackTimeHybridService
{
    private const int RetryCount = 5;

    private const int RetryDelayMilliseconds = 3000;

    public async Task ProcessHybridMatch(HybridMatch hybridMatch, IEnumerable<OddModel> odds, bool isUpdateOddSuccessfully)
    {
        if (isUpdateOddSuccessfully)
        {
            var siteDbServer = await siteDbClientService.GetActiveSiteDbAsync();
            var calculateHybridMatchTasks = siteDbServer.Select(s => CalculateHybridMatch(new CalculateHybridMatchCriteria
            {
                MatchId = hybridMatch.MatchId,
                Creator = hybridMatch.UserId,
                Action = hybridMatch.Action,
                LegOddsJSON = JsonConvert.SerializeObject(odds),
                DbServer = s.SiteName
            }));
            await Task.WhenAll(calculateHybridMatchTasks);
        }
    }

    private async Task<bool> CalculateHybridMatch(CalculateHybridMatchCriteria criteria)
    {
        await repository.ExecuteAsync(criteria);
        return criteria.ErrCode == 0;
    }

    public async Task<bool> UpdateLegOdds(IEnumerable<OddModel> odds, int userId, int matchId)
    {
        var siteDbServer = await siteDbClientService.GetActiveSiteDbAsync();

        var criteria = new UpdateLegOddsCriteria
        {
            LegOddsJSON = JsonConvert.SerializeObject(odds),
            MatchId = matchId,
            Creator = userId,
        };

        var updateOddsTasks = siteDbServer.Select(s => UpdateOdds(s.SiteName, criteria));

        var results = await Task.WhenAll(updateOddsTasks);

        return results.ToList().Exists(r => r);
    }

    public async Task<(List<OddModel> odds, List<string> transIdsInvalid)> GetOdds(int matchId, DateTimeOffset logDate, string transIds)
    {
        var retryPolicy = Policy
            .HandleResult<bool>(result => result)
            .WaitAndRetryAsync(
                RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(RetryDelayMilliseconds));

        IEnumerable<OddModel> results = null;

        await retryPolicy.ExecuteAsync(async () =>
        {
            var criteria = new HybridParlayLogSelCriteria
            {
                MatchId = matchId,
                LogDate = logDate,
                TransIds = transIds
            };

            results = await repository.FetchAsync<OddModel>(criteria);

            return results == null || !results.Any();
        });

        var inputTransIds = transIds.Split(',').Select(t => t.Trim()).ToList();

        var resultTransIds = (results ?? []).Select(r => r.TransId.ToString()).ToList();

        var transIdsMatched = inputTransIds.Where(resultTransIds.Contains).ToList();

        var transIdsInvalid = inputTransIds.Except(transIdsMatched).ToList();

        var matchedOdds = (results ?? [])
            .Where(r => transIdsMatched.Contains(r.TransId.ToString()))
            .ToList();

        return (matchedOdds, transIdsInvalid);
    }

    private async Task<bool> UpdateOdds(string siteName, UpdateLegOddsCriteria criteria)
    {
        criteria.DbServer = siteName;
        await repository.ExecuteAsync(criteria);
        return criteria.ErrCode == 0;
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
            .HandleResult<bool>(result => !result)
            .WaitAndRetryForeverAsync(
                retryAttempt => TimeSpan.FromMilliseconds(RetryDelayMilliseconds));

        var validateCommand = new HyBridValidateCriteria
        {
            MatchId = matchId,
            DbServer = siteName
        };

        IEnumerable<TranInfo> result = null;

        await retryPolicy.ExecuteAsync(async () =>
        {
            result = await repository.FetchAsync<TranInfo>(validateCommand);

            return validateCommand.ErrCode == 100 ? false : true;
        });

        return result;
    }
}
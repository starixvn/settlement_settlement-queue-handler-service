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
    Task ProcessHybridMatch(HybridMatch hybridMatch, bool isUpdateOddSuccessfully);

    Task<(List<OddModel> odds, List<string> notExistTransIds)> GetOdds(int matchId, DateTimeOffset logDate, string transIds);

    Task<bool> UpdateLegOdds(IEnumerable<OddModel> odds, int userId, int matchId);
}

public class JackTimeHybridService(
    IDynamicRepository repository,
    SiteDbClientService siteDbClientService) : IJackTimeHybridService
{
    private const int RetryCount = 20;

    private const int RetryDelayMilliseconds = 3000;

    public async Task ProcessHybridMatch(HybridMatch hybridMatch, bool isUpdateOddSuccessfully)
    {
        if (isUpdateOddSuccessfully)
        {
            var siteDbServer = await siteDbClientService.GetActiveSiteDbAsync();
            var calculateHybridMatchTasks = siteDbServer.Select(s => CalculateHybridMatch(new CalculateHybridMatchCriteria
            {
                MatchId = hybridMatch.MatchId,
                Creator = hybridMatch.UserId,
                Action = hybridMatch.Action,
                DbServer = s.SiteName
            }));
            await Task.WhenAll(calculateHybridMatchTasks);
        }
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

    public async Task<(List<OddModel> odds, List<string> notExistTransIds)> GetOdds(int matchId, DateTimeOffset logDate, string transIds)
    {
        var retryPolicy = Policy
            .HandleResult<(IEnumerable<OddModel> odds, IEnumerable<string> notExistTransIds)>(result => !result.notExistTransIds.Any())
            .WaitAndRetryAsync(
                RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(RetryDelayMilliseconds));

        var results = await retryPolicy.ExecuteAsync(async () =>
        {
            var criteria = new HybridParlayLogSelCriteria
            {
                MatchId = matchId,
                LogDate = logDate,
                TransIds = transIds
            };
            var result = await repository.FetchAsync<OddModel>(criteria);
            var invalidTransIds = result.Select(o => o.TransId.ToString()) ?? [];
            var notExistTransIds = transIds.Split(',').Except(invalidTransIds);

            return (result ?? [], notExistTransIds);
        });

        return (results.odds.ToList(), results.notExistTransIds.ToList());
    }

    private async Task<bool> UpdateOdds(string siteName, UpdateLegOddsCriteria criteria)
    {
        criteria.DbServer = siteName;
        await repository.ExecuteAsync(criteria);
        return criteria.ErrCode == 0;
    }

    private async Task<bool> CalculateHybridMatch(CalculateHybridMatchCriteria criteria)
    {
        await repository.ExecuteAsync(criteria);
        return criteria.ErrCode == 0;
    }
}
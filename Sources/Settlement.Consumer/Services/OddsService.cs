using Newtonsoft.Json;
using Polly;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.JackTimeHybrid;
using Settlement.JackTime.Criteria;
using Settlement.JackTime.Models;

namespace Settlement.Consumer.Services;

public interface OddsService
{
    Task<List<OddModel>> GetOdds(int matchId, DateTimeOffset logDate, string transIds);

    Task<ValidationTransIds> UpdateOdds(IEnumerable<OddModel> odds, string siteName, int userId, int matchId);
}

public class OddsServiceImp(IDynamicRepository repository) : OddsService
{
    private const int RetryCount = 5;

    private const int RetryDelayMilliseconds = 3000;

    public async Task<List<OddModel>> GetOdds(int matchId, DateTimeOffset logDate, string transIds)
    {
        var expectedCount = transIds.Split(',').Select(t => t.Trim()).Count(t => !string.IsNullOrWhiteSpace(t));

        var retryPolicy = Policy
            .HandleResult<bool>(result => result)
            .WaitAndRetryAsync(
                RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(RetryDelayMilliseconds));

        var results = new List<OddModel>();

        await retryPolicy.ExecuteAsync(async () =>
        {
            var criteria = new HybridParlayLogSelCriteria
            {
                MatchId = matchId,
                LogDate = logDate,
                TransIds = transIds
            };

            results = [.. (await repository.FetchAsync<OddModel>(criteria))];

            return results.Count < expectedCount;
        });

        return results;
    }

    public async Task<ValidationTransIds> UpdateOdds(IEnumerable<OddModel> odds, string siteName, int userId, int matchId)
    {
        var criteria = new UpdateLegOddsCriteria
        {
            LegOddsJSON = JsonConvert.SerializeObject(odds),
            MatchId = matchId,
            Creator = userId,
            DbServer = siteName
        };

        return await repository.GetAsync<ValidationTransIds>(criteria);
    }
}
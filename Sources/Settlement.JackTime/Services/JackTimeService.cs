using Fanex.Caching;
using Fanex.Logging;
using Fanex.SiteDbConnectionManager.Client.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Settlement.JackTime.Criteria;
using Settlement.JackTime.Models;
using Settlement.Shared.Models;

namespace Settlement.JackTime.Services;

public interface IJackTimeService
{
    Task<List<Match>> GetMatches();

    Task Consume(Match match);
}

public class JackTimeService(
    ISoccerJackTimeService soccerJackTimeService,
    IDynamicRepository repository,
    ICacheService cache,
    SiteDbClientService siteDbClientService,
    IOptions<AppSettingsKey> appSettingsKey) : IJackTimeService
{
    private const string CacheKey = "Settlement-AutoPayout-AutoInstant-LeagueId";

    public async Task Consume(Match match)
    {
        try
        {
            if (string.IsNullOrEmpty(appSettingsKey.Value.ThirdPartyUri.JackTimeTicket))
            {
                return;
            }

            await LogService.InfoAsync(
                $"Soccer JackTime Ticket Match {match.MatchId}: {JsonConvert.SerializeObject(match.VoidBetInfos)}");
            await soccerJackTimeService.UpdateTicket(match);
            await LogService.InfoAsync(
                $"Soccer JackTime Ticket Match {match.MatchId}: {JsonConvert.SerializeObject(match.VoidBetInfos)}");
        }
        catch (Exception ex)
        {
            await LogService.ErrorAsync($"Soccer JackTime Consumer Failed: MatchId({match.MatchId}) - {ex.Message}", ex);
        }
    }

    public async Task<List<Match>> GetMatches()
    {
        var matches = await GetMatchesFromRepository();

        if (matches.Count == 0)
        {
            return matches;
        }

        var (_, leagueId) = await cache.TryGetAsync<int>(CacheKey);

        return appSettingsKey.Value.EnvironmentName switch
        {
            "PRO" => leagueId > 0
                ? (from m in matches where m.LeagueId != leagueId select m).ToList()
                : matches,
            "UAT" => (from m in matches where m.LeagueId == leagueId select m).ToList(),
            _ => matches
        };
    }

    private async Task<List<Match>> GetMatchesFromRepository()
    {
        var criteria = new CashOutTicketSelCriteria();
        var siteDbServer = await siteDbClientService.GetActiveSiteDbAsync();
        var excludeDBTXN = siteDbServer.Where(s => s.SiteDbGroupId > 0);
        var getTicketInfoTasks = excludeDBTXN.Select(s => GetTicketInfo(s.SiteName, criteria));

        var results = await Task.WhenAll(getTicketInfoTasks);
        var allInfos = results.SelectMany(r => r).ToList();

        if (allInfos.Count == 0)
        {
            return [];
        }

        var grouped = allInfos.GroupBy(m => new { m.MatchId, m.LeagueId });

        return grouped.Select(group => new Match
        {
            MatchId = group.Key.MatchId,
            LeagueId = group.Key.LeagueId,
            VoidBetInfos = group.ToList()
        }).ToList();
    }

    private async Task<IEnumerable<VoidBetInfo>> GetTicketInfo(string server, CashOutTicketSelCriteria criteria)
    {
        criteria.DbServer = server;

        return await repository.FetchAsync<VoidBetInfo>(criteria);
    }
}
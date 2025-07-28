using Fanex.Caching;
using Microsoft.Extensions.Options;
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
    ICache cache,
    IOptions<AppSettingsKey> options) : IJackTimeService
{
    private const string CacheKey = "Settlement-AutoPayout-AutoInstant-LeagueId";

    public async Task Consume(Match match)
    {
        try
        {
            await soccerJackTimeService.UpdateTicket(match);
        }
        catch (Exception e)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Soccer JackTime Consumer Failed: MatchId({match.MatchId})" +
                                                      $" - {e.Message}" + $"\n {e.StackTrace}");
        }
    }

    public async Task<List<Match>> GetMatches()
    {
        var matches = await GetMatchesFromRepository();

        var (_, leagueId) = await cache.TryGetAsync<int>(CacheKey);

        return options.Value.EnvironmentName switch
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
        var siteDbServer = options.Value.SiteDbServers;
        var getTicketInfoTasks = siteDbServer.Select(s => GetTicketInfo(s, criteria));

        var results = await Task.WhenAll(getTicketInfoTasks);
        var infos = results.SelectMany(r => r);
        var grouped = infos.GroupBy(m => new
        {
            m.MatchId,
            m.LeagueId
        });

        return grouped.Select(group => new Match { MatchId = group.Key.MatchId, LeagueId = group.Key.LeagueId, VoidBetInfos = group.ToList() }).ToList();
    }

    private async Task<IEnumerable<VoidBetInfo>> GetTicketInfo(string server, CashOutTicketSelCriteria criteria)
    {
        criteria.DbServer = server;

        return await repository.FetchAsync<VoidBetInfo>(criteria);
    }
}

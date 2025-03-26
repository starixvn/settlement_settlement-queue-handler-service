using AutoMapper;
using Fanex.Caching;
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using Fanex.Settlement.MessageBridge.JackTime.Constants;
using Newtonsoft.Json;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.SoccerJackTime;
using System.Xml.Linq;

namespace Settlement.Consumer.Services;

public interface ISoccerJackTimeService
{
    Task UpdateResultMarket(SoccerResult result, IList<Market> marketOffered);

    Task UpdateTicket(SoccerResult result);
}

public class SoccerJackTimeService(
    IDynamicRepository repository,
    IMapper mapper,
    IRequestService requestService,
    ICacheService cacheService) : ISoccerJackTimeService
{
    public async Task UpdateResultMarket(SoccerResult result, IList<Market> marketOffered)
    {
        var criteria = MapToCriteria(result, marketOffered);
        var results = await repository.FetchAsync<JackTimeResult>(criteria);
        await UpdateMarkets(result.MatchId, marketOffered, results);
    }

    private CashOutSelCriteria MapToCriteria(SoccerResult result, IList<Market> marketOffered)
    {
        var criteria = mapper.Map<SoccerResult, CashOutSelCriteria>(result);
        criteria.Markets = JsonConvert.SerializeObject(marketOffered);
        return criteria;
    }

    private async Task UpdateMarkets(int matchId, IList<Market> marketOffered, IEnumerable<JackTimeResult> results)
    {
        var soccerJackTimeRequest = requestService.CreateSoccerJTRequest(marketOffered, results);
        await requestService.UpdateMarkets(soccerJackTimeRequest, marketOffered, matchId);
    }

    public async Task UpdateTicket(SoccerResult result)
    {
        var cacheKey = GetCacheKey(result);
        var ticketInfos = await GetTicketInfos(cacheKey);
        if (!ticketInfos.Any())
        {
            return;
        }

        await ProcessValidTickets(result, ticketInfos);
        await CleanupCache(cacheKey);
    }

    private static string GetCacheKey(SoccerResult result) =>
        $"{RedisJTKey.SoccerJT}-{result.MatchId}-{result.Action}";

    private async Task CleanupCache(string cacheKey)
    {
        await cacheService.RemoveAsync(cacheKey);
    }

    private async Task ProcessValidTickets(SoccerResult result, IEnumerable<VoidBetInfo> ticketInfos)
    {
        var validTicketInfos = ticketInfos.Where(FilterValidTicketInfos).ToList();
        if (validTicketInfos.Count == 0)
        {
            return;
        }

        await requestService.UpdateVoidBetTicket(new VoidBetRequest
        {
            VoidBetInfos = validTicketInfos,
            MatchId = result.MatchId,
            Action = result.Action
        }, result.MatchId);
    }

    private async Task<IEnumerable<VoidBetInfo>> GetTicketInfos(string cacheKey)
    {
        var (found, ticketInfos) = await cacheService.TryGetAsync<IEnumerable<VoidBetInfo>>(cacheKey);
        return found && ticketInfos != null ? ticketInfos : [];
    }

    private static bool FilterValidTicketInfos(VoidBetInfo voidBetInfo)
    {
        var xmlDoc = XDocument.Parse($"<root>{voidBetInfo.ExtraInfo}</root>");

        var jtseqidAttr = xmlDoc.Descendants()
            .Select(e => e.Attribute("jtseqid"))
            .FirstOrDefault(attr => attr != null);

        return jtseqidAttr != null && !string.IsNullOrEmpty(jtseqidAttr.Value);
    }
}
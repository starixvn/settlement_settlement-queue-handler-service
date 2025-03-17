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
        var criteria = mapper.Map<SoccerResult, CashOutSelCriteria>(result);
        criteria.Markets = JsonConvert.SerializeObject(marketOffered);

        var results = await repository.FetchAsync<JackTimeResult>(criteria);

        var soccerJackTimeRequest = requestService.CreateSoccerJTRequest(marketOffered, results);

        await requestService.UpdateMarkets(soccerJackTimeRequest, marketOffered, result.MatchId);
    }

    public async Task UpdateTicket(SoccerResult result)
    {
        var (found, ticketInfos) = await cacheService.TryGetAsync<IEnumerable<VoidBetInfo>>($"{RedisJTKey.SoccerJT}-{result.MatchId}-{result.Action}");

        if (!found || ticketInfos == null)
        {
            return;
        }

        var validTicketInfos = ticketInfos.Where(FilterValidTicketInfos).ToList();

        if (validTicketInfos.Any())
        {
            await requestService.UpdateVoidBetTicket(new VoidBetRequest
            {
                VoidBetInfos = validTicketInfos,
                MatchId = result.MatchId,
                Action = result.Action
            }, result.MatchId);
        }
        await cacheService.RemoveAsync($"{RedisJTKey.SoccerJT}-{result.MatchId}-{result.Action}");
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
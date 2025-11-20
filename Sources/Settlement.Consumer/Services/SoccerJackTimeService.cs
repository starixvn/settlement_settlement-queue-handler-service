using AutoMapper;
using Fanex.Settlement.MessageBridge.JackTime;
using Newtonsoft.Json;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Shared.Models;

namespace Settlement.Consumer.Services;

public interface ISoccerJackTimeService
{
    Task UpdateResultMarket(SoccerResult result, IList<Market> marketOffered);
}

public class SoccerJackTimeService(
    IDynamicRepository repository,
    IMapper mapper,
    IRequestService requestService) : ISoccerJackTimeService
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
}
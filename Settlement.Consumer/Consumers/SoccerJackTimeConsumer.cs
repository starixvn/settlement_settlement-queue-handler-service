using AutoMapper;
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;

namespace Settlement.Consumer.Consumers;
public class SoccerJackTimeConsumer(
    IRequestMarketService requestService,
    IDynamicRepository repository,
    IMapper mapper,
    IOptions<AppSettingsKey> options) : IConsumer<SoccerResult>
{
    private readonly AppSettingsKey appSettingsKey = options.Value;

    public async Task Consume(ConsumeContext<SoccerResult> context)
    {
        var marketOffered = await requestService.GetMarkets(context.Message.MatchId);
        if (!marketOffered.Any())
        {
            return;
        }

        await UpdateResultMarket(context.Message, marketOffered);

    }

    private async Task UpdateResultMarket(SoccerResult result, IEnumerable<Market> marketOffered)
    {
        var criteria = mapper.Map<SoccerResult, CashOutSelCriteria>(result);
        criteria.Markets = JsonConvert.SerializeObject(marketOffered);

        var tasks = appSettingsKey.SiteDbServer.Select(s => GetResult(s, criteria)).ToList();

        var results = await Task.WhenAll(tasks.ToArray());

        var soccerJackTimeRequest = requestService.CreateSoccerJTRequest(marketOffered, results.SelectMany(r => r));


        if (soccerJackTimeRequest.Request.Markets.Any())
            await requestService.UpdateMarkets(soccerJackTimeRequest);
    }

    private async Task<IEnumerable<JackTimeResult>> GetResult(string dbServer, CashOutSelCriteria criteria)
    {
        criteria.DbServer = dbServer;
        return await repository.FetchAsync<JackTimeResult>(criteria);
    }


}

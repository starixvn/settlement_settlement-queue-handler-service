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
        try
        {
            var marketOffered = await requestService.GetMarkets(context.Message.MatchId);
            if (!marketOffered.Any())
            {
                return;
            }

            await UpdateResultMarket(context.Message, marketOffered);
        }
        catch (Exception e)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Soccer JackTime Update Market Failed: {e.Message}");
        }
    }

    private async Task UpdateResultMarket(SoccerResult result, IEnumerable<Market> marketOffered)
    {
        var criteria = mapper.Map<SoccerResult, CashOutSelCriteria>(result);
        criteria.Markets = JsonConvert.SerializeObject(marketOffered);

        var results = await repository.FetchAsync<JackTimeResult>(criteria);

        var soccerJackTimeRequest = requestService.CreateSoccerJTRequest(marketOffered, results);

        await requestService.UpdateMarkets(soccerJackTimeRequest);
    }
}

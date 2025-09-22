using Fanex.Data.Repository;
using Fanex.Logging;
using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Services;
using Settlement.Shared.Models;

namespace Settlement.Consumer.Consumers;

public class SoccerJackTimeConsumer(
    IRequestService requestService,
    ISoccerJackTimeService soccerJackTimeService,
    IOptions<AppSettingsKey> options,
    IDynamicRepository repository) : IConsumer<SoccerResult>
{
    private const int LogId = 81062;

    public async Task Consume(ConsumeContext<SoccerResult> context)
    {
        try
        {
            if (string.IsNullOrEmpty(options.Value.ThirdPartyUri.JackTimeMarkets))
            {
                return;
            }

            var marketOffered = await requestService.GetMarkets(context.Message.MatchId);
            await LogMarket(marketOffered, context.Message);
            if (marketOffered.Count == 0)
            {
                return;
            }

            await soccerJackTimeService.UpdateResultMarket(context.Message, marketOffered);
        }
        catch (HttpRequestException exception)
        {
            await LogAPIFailed(context.Message);
            await LogService.ErrorAsync($"Soccer JackTime Result API Failed: MatchId({context.Message.MatchId}) Action({context.Message.Action})" +
                                        $" - {exception.StatusCode} - {exception.Message}");
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync($"Soccer JackTime Consumer Failed: MatchId({context.Message.MatchId}) Action({context.Message.Action})" +
                                                      $" - {e.Message}" + $"\n {e.StackTrace}");
        }
    }


    private async Task LogMarket(IEnumerable<Market> markets, SoccerResult context)
    {
        var message = $"Action: {context.Action}, Markets: {JsonConvert.SerializeObject(markets)}";
        var criteria = new LogCriteria(context.MatchId, message, LogId);
        await repository.ExecuteAsync(criteria);
    }

    private async Task LogAPIFailed(SoccerResult context)
    {
        var message = $"Cash out Jack Time Failed: Action ({context.Action})";
        var criteria = new LogCriteria(context.MatchId, message, LogId);
        await repository.ExecuteAsync(criteria);
    }
}
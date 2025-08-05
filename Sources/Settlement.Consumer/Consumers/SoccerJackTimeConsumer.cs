using Fanex.Logging;
using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Settlement.Consumer.Services;
using Settlement.Shared.Models;

namespace Settlement.Consumer.Consumers;

public class SoccerJackTimeConsumer(
    IRequestService requestService,
    ISoccerJackTimeService soccerJackTimeService,
    IOptions<AppSettingsKey> options) : IConsumer<SoccerResult>
{
    public async Task Consume(ConsumeContext<SoccerResult> context)
    {
        try
        {
            if (string.IsNullOrEmpty(options.Value.ThirdPartyUri.JackTimeMarkets))
            {
                return;
            }

            var marketOffered = await requestService.GetMarkets(context.Message.MatchId);
            await LogService.InfoAsync($"Match {context.Message.MatchId} - Markets: " + JsonConvert.SerializeObject(marketOffered));
            if (marketOffered.Count == 0)
            {
                return;
            }

            await soccerJackTimeService.UpdateResultMarket(context.Message, marketOffered);
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync($"Soccer JackTime Consumer Failed: MatchId({context.Message.MatchId}) Action({context.Message.Action})" +
                                                      $" - {e.Message}" + $"\n {e.StackTrace}");
        }
    }
}
using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using Settlement.Consumer.Services;

namespace Settlement.Consumer.Consumers;
public class SoccerJackTimeConsumer(
    IRequestService requestService,
    ISoccerJackTimeService soccerJackTimeService) : IConsumer<SoccerResult>
{
    public async Task Consume(ConsumeContext<SoccerResult> context)
    {
        try
        {
            var marketOffered = await requestService.GetMarkets(context.Message.MatchId);
            if (!marketOffered.Any())
            {
                return;
            }

            await soccerJackTimeService.UpdateTicket(context.Message);
            await soccerJackTimeService.UpdateResultMarket(context.Message, marketOffered);
        }
        catch (Exception e)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Soccer JackTime Consumer Failed: MatchId({context.Message.MatchId}) Action({context.Message.Action}" +
                                                      $" - {e.Message}");
        }
    }
}

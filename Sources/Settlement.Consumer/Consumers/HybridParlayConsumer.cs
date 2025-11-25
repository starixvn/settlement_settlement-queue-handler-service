using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using Settlement.Consumer.Constants;
using Settlement.Consumer.Services;
using System.Data;

namespace Settlement.Consumer.Consumers;

public class HybridParlayConsumer(
    HybridParlayService hybridParlayService,
    HybridParlayLogService hybridParlayLogService) : IConsumer<HybridMatch>
{
    public async Task Consume(ConsumeContext<HybridMatch> context)
    {
        try
        {
            var hybridMatch = context.Message;
            var matchId = hybridMatch.MatchId;

            var originalTransId = await hybridParlayService.GetHyBridTransId(matchId, hybridMatch.Action);
            await LogService.InfoAsync($"JackTime Hybrid GetHyBridTransId ({originalTransId.Count}) - MatchId {matchId}");

            if (originalTransId.Count > 0)
            {
                var (Odds, InvalidTransId) = await hybridParlayService.GetOddsAndInvalidTransId(matchId, hybridMatch.LogDate, originalTransId);

                await hybridParlayLogService.InvalidTransIdsFetchNewOdds(hybridMatch, InvalidTransId, originalTransId);
                await hybridParlayService.UpdateOddsAndSettle(hybridMatch, Odds, originalTransId);
            }
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync($"JackTime Hybrid Consumer Failed: MatchId({context.Message.MatchId}) - {e.Message}\n{e.StackTrace}");
            throw;
        }
    }
}

public class JackTimeHybridConsumerDefinition : ConsumerDefinition<HybridParlayConsumer>
{
    private const int RetryInterval = 5000;
    private const int RetryCount = 3;
    private const int FetchItemNumber = 10;
    private const string IgnoreMessage = "SQL";

    public JackTimeHybridConsumerDefinition()
    {
        EndpointName = ConsumeType.JackTimeHybrid;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<HybridParlayConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        if (endpointConfigurator is not IRabbitMqReceiveEndpointConfigurator rmq)
        {
            return;
        }

        rmq.PrefetchCount = FetchItemNumber;
        rmq.DiscardSkippedMessages();
        rmq.DiscardFaultedMessages();
        rmq.UseMessageRetry(retryConfig =>
        {
            retryConfig.Ignore<ArgumentNullException>();
            retryConfig.Ignore<DataException>(x => x.Message.Contains(IgnoreMessage));
            retryConfig.Interval(RetryCount, TimeSpan.FromMilliseconds(RetryInterval));
        });
    }
}
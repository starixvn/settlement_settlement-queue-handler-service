using Fanex.Logging;
using MassTransit;
using Settlement.Consumer.Constants;
using Settlement.Consumer.Models.JackTimeHybrid;
using Settlement.Consumer.Services;
using System.Data;

namespace Settlement.Consumer.Consumers;

public class JackTimeHybridConsumer(
    IJackTimeHybridService jackTimeHybridService) : IConsumer<HybridMatch>
{
    public async Task Consume(ConsumeContext<HybridMatch> context)
    {
        try
        {
            var odds = await jackTimeHybridService.GetOdds(context.Message.MatchId, context.Message.LogDate);
            var isUpdateOddSuccessfully = await jackTimeHybridService.UpdateLegOdds(odds, context.Message.UserId, context.Message.MatchId);
            await jackTimeHybridService.ProcessHybridMatch(context.Message, isUpdateOddSuccessfully);
            await LogService.InfoAsync($"JackTime Hybrid Consumer Succeeded: MatchId({context.Message.MatchId})");
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync($"JackTime Hybrid Consumer Failed: MatchId({context.Message.MatchId})" +
                                      $" - {e.Message}\n{e.StackTrace}");
        }
    }
}

public class JackTimeHybridConsumerDefinition : ConsumerDefinition<JackTimeHybridConsumer>
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
        IConsumerConfigurator<JackTimeHybridConsumer> consumerConfigurator,
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


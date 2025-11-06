using Fanex.Logging;
using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using Newtonsoft.Json;
using Settlement.Consumer.Constants;
using Settlement.Consumer.Services;
using System.Data;

namespace Settlement.Consumer.Consumers;

public class JackTimeHybridConsumer(
    IJackTimeHybridService jackTimeHybridService, IHybridLogService hybridLogService) : IConsumer<HybridMatch>
{
    public async Task Consume(ConsumeContext<HybridMatch> context)
    {
        try
        {
            await LogService.InfoAsync($"JackTime Hybrid Consumer Started: MatchId({context.Message.MatchId})");
            var (odds, notExistTransIds) = await jackTimeHybridService.GetOdds(context.Message.MatchId, context.Message.LogDate, context.Message.TransId);
            //if (notExistTransIds.Any())
            //{
            //    var logId = await hybridLogService.WriteLog(context.Message.MatchId, "No Odds Found");
            //    await hybridLogService.NotExistLegOdds(context.Message.MatchId, logId, notExistTransIds);
            //}

            //if (odds.Count == 0)
            //{
            //    return;
            //}

            await LogService.InfoAsync($"JackTime Hybrid Consumer Get Odds: MatchId({context.Message.MatchId}) - {JsonConvert.SerializeObject(odds)}");


            var isUpdateOddSuccessfully = await jackTimeHybridService.UpdateLegOdds(odds, context.Message.UserId, context.Message.MatchId);


            await LogService.InfoAsync($"JackTime Hybrid Consumer Update Leg Odds: MatchId({context.Message.MatchId}) - {isUpdateOddSuccessfully}");


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


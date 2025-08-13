using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using NSubstitute;
using Settlement.Consumer.Consumers;
using Settlement.Consumer.Services;
using Settlement.Shared.Models;
using Microsoft.Extensions.Options;

namespace Settlement.Consumer.UnitTests.Consumers;

public class SoccerJackTimeConsumerTests
{
    private readonly IRequestService requestService;
    private readonly ISoccerJackTimeService soccerJackTimeService;
    private readonly SoccerJackTimeConsumer soccerJackTimeConsumer;
    private readonly ConsumeContext<SoccerResult> context;

    public SoccerJackTimeConsumerTests()
    {
        requestService = Substitute.For<IRequestService>();
        soccerJackTimeService = Substitute.For<ISoccerJackTimeService>();
        context = Substitute.For<ConsumeContext<SoccerResult>>();
        var options = Options.Create(new AppSettingsKey
        {
            EnvironmentName = "LOCAL",
            ThirdPartyUri = new ThirdPartySettingKeys
            {
                JackTimeMarkets = "https://example.com/markets"
            }
        });
        soccerJackTimeConsumer = new SoccerJackTimeConsumer(requestService, soccerJackTimeService, options);
        context.Message.Returns(new SoccerResult { MatchId = 1 });
    }

    [Fact]
    public async Task Consume_ShouldNotCallUpdateMethods_WhenNoMarketsOffered()
    {
        // Arrange
        requestService.GetMarkets(Arg.Any<int>()).Returns(Task.FromResult(new List<Market>()));

        // Act
        await soccerJackTimeConsumer.Consume(context);

        // Assert
        await soccerJackTimeService.DidNotReceive().UpdateResultMarket(Arg.Any<SoccerResult>(), Arg.Any<List<Market>>());
    }

    [Fact]
    public async Task Consume_ShouldCallUpdateMethods_WhenMarketsOffered()
    {
        // Arrange
        var markets = new List<Market> { new Market() };
        requestService.GetMarkets(Arg.Any<int>()).Returns(Task.FromResult(markets));

        // Act
        await soccerJackTimeConsumer.Consume(context);

        // Assert
        await soccerJackTimeService.Received(1).UpdateResultMarket(Arg.Any<SoccerResult>(), markets);
    }
}
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using Microsoft.Extensions.Options;
using NSubstitute;
using Settlement.Consumer.Consumers;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Services;
using Settlement.Shared.Models;

namespace Settlement.Consumer.UnitTests.Consumers;

public class SoccerJackTimeConsumerTests
{
    private readonly IRequestService requestService;
    private readonly ISoccerJackTimeService soccerJackTimeService;
    private readonly SoccerJackTimeConsumer soccerJackTimeConsumer;
    private readonly ConsumeContext<SoccerResult> context;
    private readonly IDynamicRepository repository;

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
        repository = Substitute.For<IDynamicRepository>();
        soccerJackTimeConsumer = new SoccerJackTimeConsumer(requestService, soccerJackTimeService, options, repository);
        context.Message.Returns(new SoccerResult { MatchId = 1, Action = "TestAction" });
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

    [Fact]
    public async Task Consume_ShouldReturnEarly_WhenJackTimeMarketsUriIsEmpty()
    {
        // Arrange
        var emptyAppSettings = new AppSettingsKey
        {
            EnvironmentName = "LOCAL",
            ThirdPartyUri = new ThirdPartySettingKeys
            {
                JackTimeMarkets = ""
            }
        };
        var options = Options.Create(emptyAppSettings);
        var consumer = new SoccerJackTimeConsumer(requestService, soccerJackTimeService, options, repository);

        // Act
        await consumer.Consume(context);

        // Assert
        await requestService.DidNotReceive().GetMarkets(Arg.Any<int>());
        await soccerJackTimeService.DidNotReceive().UpdateResultMarket(Arg.Any<SoccerResult>(), Arg.Any<List<Market>>());
    }

    [Fact]
    public async Task Consume_ShouldLogMarket_WhenMarketsAreRetrieved()
    {
        // Arrange
        var markets = new List<Market> { new Market() };
        requestService.GetMarkets(Arg.Any<int>()).Returns(Task.FromResult(markets));

        // Act
        await soccerJackTimeConsumer.Consume(context);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Any<LogCriteria>());
    }

    [Fact]
    public async Task Consume_ShouldHandleException_WhenRequestServiceThrowsException()
    {
        // Arrange
        var exception = new Exception("Test exception");
        requestService.GetMarkets(Arg.Any<int>()).Returns(Task.FromException<List<Market>>(exception));

        // Act
        await soccerJackTimeConsumer.Consume(context);

        // Assert
        await soccerJackTimeService.DidNotReceive().UpdateResultMarket(Arg.Any<SoccerResult>(), Arg.Any<List<Market>>());
    }
}
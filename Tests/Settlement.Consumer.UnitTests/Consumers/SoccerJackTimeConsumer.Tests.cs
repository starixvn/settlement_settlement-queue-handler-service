using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using NSubstitute;
using Settlement.Consumer.Consumers;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;

public class SoccerJackTimeConsumerTests
{
    private readonly IRequestService _mockRequestService;
    private readonly ISoccerJackTimeService _mockSoccerJackTimeService;
    private readonly SoccerJackTimeConsumer _consumer;
    private readonly ConsumeContext<SoccerResult> _context;

    public SoccerJackTimeConsumerTests()
    {
        _mockRequestService = Substitute.For<IRequestService>();
        _mockSoccerJackTimeService = Substitute.For<ISoccerJackTimeService>();
        _context = Substitute.For<ConsumeContext<SoccerResult>>();
        _consumer = new SoccerJackTimeConsumer(_mockRequestService, _mockSoccerJackTimeService);
        _context.Message.Returns(new SoccerResult { MatchId = 1 });
    }

    [Fact]
    public async Task Consume_ShouldNotCallUpdateMethods_WhenNoMarketsOffered()
    {
        // Arrange
        _mockRequestService.GetMarkets(Arg.Any<int>()).Returns(Task.FromResult(new List<Market>()));

        // Act
        await _consumer.Consume(_context);

        // Assert
        await _mockSoccerJackTimeService.DidNotReceive().UpdateTicket(Arg.Any<SoccerResult>());
        await _mockSoccerJackTimeService.DidNotReceive().UpdateResultMarket(Arg.Any<SoccerResult>(), Arg.Any<List<Market>>());
    }

    [Fact]
    public async Task Consume_ShouldCallUpdateMethods_WhenMarketsOffered()
    {
        // Arrange
        var markets = new List<Market> { new Market() };
        _mockRequestService.GetMarkets(Arg.Any<int>()).Returns(Task.FromResult(markets));

        // Act
        await _consumer.Consume(_context);

        // Assert
        await _mockSoccerJackTimeService.Received(1).UpdateTicket(Arg.Any<SoccerResult>());
        await _mockSoccerJackTimeService.Received(1).UpdateResultMarket(Arg.Any<SoccerResult>(), markets);
    }
}

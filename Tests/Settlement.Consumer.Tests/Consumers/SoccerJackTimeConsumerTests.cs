using AutoMapper;
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using NSubstitute;
using Settlement.Consumer.Consumers;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;

namespace Settlement.Consumer.Tests
{
    public class SoccerJackTimeConsumerTests
    {
        private readonly IRequestService _requestService;
        private readonly IDynamicRepository _repository;
        private readonly IMapper _mapper;
        private readonly SoccerJackTimeConsumer _consumer;

        public SoccerJackTimeConsumerTests()
        {
            _requestService = Substitute.For<IRequestService>();
            _repository = Substitute.For<IDynamicRepository>();
            _mapper = Substitute.For<IMapper>();

            _consumer = new SoccerJackTimeConsumer(_requestService, _repository, _mapper);
        }

        [Fact]
        public async Task Consume_ShouldCallGetMarkets()
        {
            // Arrange
            var context = Substitute.For<ConsumeContext<SoccerResult>>();
            context.Message.Returns(new SoccerResult { MatchId = 1 });

            _requestService.GetMarkets(1).Returns(new List<Market> { new Market() });

            // Act
            await _consumer.Consume(context);

            // Assert
            await _requestService.Received(1).GetMarkets(1);
        }

        [Fact]
        public async Task Consume_ShouldNotCallUpdateTicket_WhenNoMarketsOffered()
        {
            // Arrange
            var context = Substitute.For<ConsumeContext<SoccerResult>>();
            context.Message.Returns(new SoccerResult { MatchId = 1 });

            _requestService.GetMarkets(1).Returns(new List<Market>());

            // Act
            await _consumer.Consume(context);

            // Assert
            await _requestService.DidNotReceive().UpdateVoidBetTicket(Arg.Any<VoidBetRequest>(), Arg.Any<int>());
        }

        [Fact]
        public async Task Consume_ShouldCallUpdateResultMarket()
        {
            // Arrange
            var context = Substitute.For<ConsumeContext<SoccerResult>>();
            var result = new SoccerResult { MatchId = 1 };
            var marketOffered = new List<Market> { new Market() };
            var criteria = new CashOutSelCriteria();
            var jackTimeResults = new List<JackTimeResult> { new JackTimeResult() };
            var requestModel = new JTResultMarketRequestModel();

            context.Message.Returns(result);
            _requestService.GetMarkets(1).Returns(marketOffered);
            _mapper.Map<SoccerResult, CashOutSelCriteria>(result).Returns(criteria);
            _repository.FetchAsync<JackTimeResult>(criteria).Returns(jackTimeResults);
            _requestService.CreateSoccerJTRequest(marketOffered, jackTimeResults).Returns(requestModel);

            // Act
            await _consumer.Consume(context);

            // Assert
            await _requestService.Received(1).UpdateMarkets(requestModel, marketOffered, 1);
        }

        [Fact]
        public async Task Consume_ShouldNotCallUpdateVoidBetTicket_WhenActionContainsCAL()
        {
            // Arrange
            var context = Substitute.For<ConsumeContext<SoccerResult>>();
            var result = new SoccerResult { MatchId = 1, Action = "CAL" };
            var marketOffered = new List<Market> { new Market() };

            context.Message.Returns(result);
            _requestService.GetMarkets(1).Returns(marketOffered);

            // Act
            await _consumer.Consume(context);

            // Assert
            await _requestService.DidNotReceive().UpdateVoidBetTicket(Arg.Any<VoidBetRequest>(), Arg.Any<int>());
        }

        [Fact]
        public async Task Consume_ShouldCallUpdateVoidBetTicket_WhenTicketInfoExists()
        {
            // Arrange
            var context = Substitute.For<ConsumeContext<SoccerResult>>();
            var result = new SoccerResult { MatchId = 1, Action = "REFUND" };
            var marketOffered = new List<Market> { new Market() };
            var criteria = new SoccerGetTicketInfoCriteria();
            var ticketInfo = new List<VoidBetInfo> { new VoidBetInfo() };

            context.Message.Returns(result);
            _requestService.GetMarkets(1).Returns(marketOffered);
            _mapper.Map<SoccerResult, SoccerGetTicketInfoCriteria>(result).Returns(criteria);
            _repository.FetchAsync<VoidBetInfo>(criteria).Returns(ticketInfo);

            // Act
            await _consumer.Consume(context);

            // Assert
            await _requestService.Received(1).UpdateVoidBetTicket(Arg.Is<VoidBetRequest>(r => r.MatchId == 1 && r.Action == "REFUND" && r.VoidBetInfos == ticketInfo), 1);
        }
    }
}

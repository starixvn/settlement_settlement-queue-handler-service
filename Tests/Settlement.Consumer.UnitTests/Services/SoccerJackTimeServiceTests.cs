using AutoMapper;
using Fanex.Caching;
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;

namespace Settlement.Consumer.Tests.Services
{
    public class SoccerJackTimeServiceTests
    {
        private readonly IDynamicRepository _repositoryMock;
        private readonly IMapper _mapperMock;
        private readonly IRequestService _requestServiceMock;
        private readonly ICacheService _cacheServiceMock;
        private readonly SoccerJackTimeService _service;

        public SoccerJackTimeServiceTests()
        {
            _repositoryMock = Substitute.For<IDynamicRepository>();
            _mapperMock = Substitute.For<IMapper>();
            _requestServiceMock = Substitute.For<IRequestService>();
            _cacheServiceMock = Substitute.For<ICacheService>();
            _service = new SoccerJackTimeService(
                _repositoryMock,
                _mapperMock,
                _requestServiceMock,
                _cacheServiceMock);
        }

        [Fact]
        public async Task UpdateResultMarket_ShouldCallUpdateMarkets_WhenResultsAreFetched()
        {
            // Arrange
            var result = new SoccerResult { MatchId = 1 };
            var marketOffered = new List<Market>();
            var criteria = new CashOutSelCriteria();
            var jackTimeResults = new List<JackTimeResult>();

            _mapperMock.Map<SoccerResult, CashOutSelCriteria>(result).Returns(criteria);
            _repositoryMock.FetchAsync<JackTimeResult>(criteria).Returns(Task.FromResult((IEnumerable<JackTimeResult>)jackTimeResults));
            _requestServiceMock.CreateSoccerJTRequest(marketOffered, jackTimeResults).Returns(new JTResultMarketRequestModel());

            // Act
            await _service.UpdateResultMarket(result, marketOffered);

            // Assert
            await _requestServiceMock.Received(1).UpdateMarkets(Arg.Any<JTResultMarketRequestModel>(), marketOffered, result.MatchId);
        }

        [Fact]
        public async Task UpdateTicket_ShouldNotCallUpdateVoidBetTicket_WhenCacheNotFound()
        {
            // Arrange
            var result = new SoccerResult { MatchId = 1, Action = "Action" };
            _cacheServiceMock.TryGetAsync<IEnumerable<VoidBetInfo>>(Arg.Any<string>()).Returns(Task.FromResult((false, null as IEnumerable<VoidBetInfo>)));

            // Act
            await _service.UpdateTicket(result);

            // Assert
            await _requestServiceMock.DidNotReceive().UpdateVoidBetTicket(Arg.Any<VoidBetRequest>(), result.MatchId);
        }

        [Fact]
        public async Task UpdateTicket_ShouldCallUpdateVoidBetTicket_WhenValidTicketInfosFound()
        {
            // Arrange
            var result = new SoccerResult { MatchId = 1, Action = "Action" };
            var ticketInfos = new List<VoidBetInfo>
            {
                new VoidBetInfo { ExtraInfo = "<s25 jtseqid='123' />" }
            };

            _cacheServiceMock.TryGetAsync<IEnumerable<VoidBetInfo>>(Arg.Any<string>()).Returns((true, ticketInfos));

            // Act
            await _service.UpdateTicket(result);

            // Assert
            await _requestServiceMock.Received(1).UpdateVoidBetTicket(Arg.Any<VoidBetRequest>(), result.MatchId);
        }


        [Fact]
        public async Task FilterValidTicketInfos_ShouldReturnFalse_WhenJtseqidIsInvalid()
        {
            // Arrange
            var result = new SoccerResult { MatchId = 1, Action = "Action" };
            var ticketInfos = new List<VoidBetInfo>
            {
                new VoidBetInfo { ExtraInfo = "<s25 s5='123' />" }
            };

            _cacheServiceMock.TryGetAsync<IEnumerable<VoidBetInfo>>(Arg.Any<string>()).Returns((true, ticketInfos));

            // Act
            await _service.UpdateTicket(result);

            // Assert
            await _requestServiceMock.DidNotReceive().UpdateVoidBetTicket(Arg.Any<VoidBetRequest>(), result.MatchId);
        }
    }
}
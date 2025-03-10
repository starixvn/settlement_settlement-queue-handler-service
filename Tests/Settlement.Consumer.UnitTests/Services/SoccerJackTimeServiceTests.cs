using AutoMapper;
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using Microsoft.Extensions.Options;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;

namespace Settlement.Consumer.UnitTests.Services
{
    public class SoccerJackTimeServiceTests
    {
        private readonly IDynamicRepository _mockRepository;
        private readonly IMapper _mockMapper;
        private readonly IRequestService _mockRequestService;
        private readonly IOptions<AppSettingsKey> _mockOptions;
        private readonly SoccerJackTimeService _service;

        public SoccerJackTimeServiceTests()
        {
            _mockRepository = Substitute.For<IDynamicRepository>();
            _mockMapper = Substitute.For<IMapper>();
            _mockRequestService = Substitute.For<IRequestService>();
            _mockOptions = Substitute.For<IOptions<AppSettingsKey>>();
            _mockOptions.Value.Returns(new AppSettingsKey { SiteDbServer = new List<string> { "Server1", "Server2" } });

            _service = new SoccerJackTimeService(_mockRepository, _mockMapper, _mockRequestService, _mockOptions);
        }

        [Fact]
        public async Task UpdateResultMarket_ShouldCallUpdateMarkets()
        {
            // Arrange
            var result = new SoccerResult();
            var marketOffered = new List<Market> { new Market() };
            var criteria = new CashOutSelCriteria();
            var jackTimeResults = new List<JackTimeResult> { new JackTimeResult() };
            var requestModel = new JTResultMarketRequestModel();

            _mockMapper.Map<SoccerResult, CashOutSelCriteria>(result).Returns(criteria);
            _mockRepository.FetchAsync<JackTimeResult>(criteria).Returns(Task.FromResult(jackTimeResults.AsEnumerable()));
            _mockRequestService.CreateSoccerJTRequest(marketOffered, jackTimeResults).Returns(requestModel);

            // Act
            await _service.UpdateResultMarket(result, marketOffered);

            // Assert
            await _mockRequestService.Received(1).UpdateMarkets(requestModel, marketOffered, result.MatchId);
        }

        [Fact]
        public async Task UpdateTicket_ShouldNotCallUpdateVoidBetTicket_WhenActionContainsCAL()
        {
            // Arrange
            var result = new SoccerResult { Action = "CAL" };

            // Act
            await _service.UpdateTicket(result);

            // Assert
            await _mockRequestService.DidNotReceive().UpdateVoidBetTicket(Arg.Any<VoidBetRequest>(), Arg.Any<int>());
        }

        [Fact]
        public async Task UpdateTicket_ShouldCallUpdateVoidBetTicket_WhenTicketsExist()
        {
            // Arrange
            var result = new SoccerResult { Action = "ACTION", MatchId = 1 };
            var criteria = new SoccerGetTicketInfoCriteria();
            var voidBetInfos = new List<VoidBetInfo> { new VoidBetInfo { ExtraInfo = "<root jtseqid='123'></root>" } };

            _mockMapper.Map<SoccerResult, SoccerGetTicketInfoCriteria>(result).Returns(criteria);
            _mockRepository.FetchAsync<VoidBetInfo>(Arg.Any<SoccerGetTicketInfoCriteria>()).Returns(Task.FromResult(voidBetInfos.AsEnumerable()));

            // Act
            await _service.UpdateTicket(result);

            // Assert
            await _mockRequestService.Received(1).UpdateVoidBetTicket(Arg.Any<VoidBetRequest>(), result.MatchId);
        }

        [Fact]
        public async Task UpdateTicket_ShouldNotCallUpdateVoidBetTicket_WhenNoTicketsExist()
        {
            // Arrange
            var result = new SoccerResult { Action = "ACTION", MatchId = 1 };
            var criteria = new SoccerGetTicketInfoCriteria();
            var voidBetInfos = new List<VoidBetInfo> { new VoidBetInfo { ExtraInfo = "<root></root>" } };

            _mockMapper.Map<SoccerResult, SoccerGetTicketInfoCriteria>(result).Returns(criteria);
            _mockRepository.FetchAsync<VoidBetInfo>(Arg.Any<SoccerGetTicketInfoCriteria>()).Returns(Task.FromResult(voidBetInfos.AsEnumerable()));

            // Act
            await _service.UpdateTicket(result);

            // Assert
            await _mockRequestService.DidNotReceive().UpdateVoidBetTicket(Arg.Any<VoidBetRequest>(), result.MatchId);
        }
    }
}

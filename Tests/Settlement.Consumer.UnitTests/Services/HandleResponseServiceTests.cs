using AutoMapper;
using Fanex.Data.Repository;
using Fanex.RemotingRef;
using Fanex.Settlement.MessageBridge.JackTime;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;

namespace Settlement.Consumer.UnitTests.Services
{
    public class HandleResponseServiceTests
    {
        private readonly SendMailService _mockSendMailService;
        private readonly IMapper _mockMapper;
        private readonly IRemotingRefService _mockRemotingRefService;
        private readonly IDynamicRepository _mockRepository;
        private readonly HandleResponseService _service;

        public HandleResponseServiceTests()
        {
            _mockSendMailService = Substitute.For<SendMailService>();
            _mockMapper = Substitute.For<IMapper>();
            _mockRemotingRefService = Substitute.For<IRemotingRefService>();
            _mockRepository = Substitute.For<IDynamicRepository>();

            _service = new HandleResponseService(_mockSendMailService, _mockMapper, _mockRemotingRefService, _mockRepository);
        }

        [Fact]
        public async Task HandleVoidBetResponse_ShouldSendMail_WhenResponseIsNotSuccessful()
        {
            // Arrange
            var response = new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.BadRequest };
            var model = new VoidBetApiModel { VoidBetInfos = new List<VoidBetInfo> { new() } };
            var matchId = 1;
            var matchDetail = new MatchDetail { MatchId = matchId };
            var notificationModel = new NotificationModel();

            _mockRepository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>()).Returns(Task.FromResult(matchDetail));
            _mockMapper.Map<MatchDetail, NotificationModel>(matchDetail).Returns(notificationModel);
            response.Content = new StringContent("Error message");

            // Act
            await _service.HandleVoidBetResponse(response, model, matchId);

            // Assert
            _mockSendMailService.Received(1).SendMail(Arg.Is<NotificationModel>(nm => nm.Message == "Error message" && nm.VoidBetInfos == model.VoidBetInfos), true);
        }

        [Fact]
        public async Task HandleVoidBetResponse_ShouldNotSendMail_WhenResponseIsSuccessful()
        {
            // Arrange
            var response = new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK };
            var model = new VoidBetApiModel { VoidBetInfos = new List<VoidBetInfo> { new VoidBetInfo() } };
            var matchId = 1;

            // Act
            await _service.HandleVoidBetResponse(response, model, matchId);

            // Assert
            _mockSendMailService.DidNotReceive().SendMail(Arg.Any<NotificationModel>(), true);
        }

        [Fact]
        public async Task HandleUpdateResultResponse_ShouldSendMail_WhenResponseIsNotSuccessful()
        {
            // Arrange
            var response = new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.BadRequest };
            var markets = new List<Market> { new Market() };
            var matchId = 1;
            var matchDetail = new MatchDetail { MatchId = matchId };
            var notificationModel = new NotificationModel();

            _mockRepository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>()).Returns(Task.FromResult(matchDetail));
            _mockMapper.Map<MatchDetail, NotificationModel>(matchDetail).Returns(notificationModel);
            response.Content = new StringContent("Error message");

            // Act
            await _service.HandleUpdateResultResponse(response, markets, matchId);

            // Assert
            _mockSendMailService.Received(1).SendMail(Arg.Is<NotificationModel>(nm => nm.Message == "Error message" && nm.Markets == markets), false);
        }

        [Fact]
        public async Task HandleUpdateResultResponse_ShouldNotSendMail_WhenResponseIsSuccessful()
        {
            // Arrange
            var response = new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK };
            var markets = new List<Market> { new Market() };
            var matchId = 1;

            // Act
            await _service.HandleUpdateResultResponse(response, markets, matchId);

            // Assert
            _mockSendMailService.DidNotReceive().SendMail(Arg.Any<NotificationModel>(), false);
        }
    }
}

using Fanex.Settlement.MessageBridge.JackTime;
using Microsoft.Extensions.Options;
using NSubstitute;
using Settlement.Consumer.Models;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;

namespace Settlement.Consumer.UnitTests.Services
{
    public class RequestServiceTests
    {
        private readonly IInvokeExternalAPI _mockInvokeExternalApi;
        private readonly IOptions<AppSettingsKey> _mockOptions;
        private readonly IHandleResponseService _mockHandleResponseService;
        private readonly RequestService _service;

        public RequestServiceTests()
        {
            _mockInvokeExternalApi = Substitute.For<IInvokeExternalAPI>();
            _mockOptions = Substitute.For<IOptions<AppSettingsKey>>();
            _mockHandleResponseService = Substitute.For<IHandleResponseService>();
            _mockOptions.Value.Returns(new AppSettingsKey
            {
                ThirdPartyUri = new ThirdPartySettingKeys
                {
                    JackTimeMarkets = "http://example.com/markets",
                    JackTimeResultMarkets = "http://example.com/resultmarkets",
                    JackTimeTicket = "http://example.com/ticket"
                }
            });

            _service = new RequestService(_mockInvokeExternalApi, _mockOptions, _mockHandleResponseService);
        }

        [Fact]
        public async Task GetMarkets_ShouldReturnMarkets_WhenApiCallIsSuccessful()
        {
            // Arrange
            var matchId = 1;
            var soccerMarketItem = new SoccerMarketItem
            {
                IsSuccess = true,
                Data = new List<Market> { new() }
            };
            _mockInvokeExternalApi.GetAsync<SoccerMarketItem>(Arg.Any<string>())!.Returns(Task.FromResult(soccerMarketItem));

            // Act
            var result = await _service.GetMarkets(matchId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetMarkets_ShouldReturnEmptyList_WhenApiCallFails()
        {
            // Arrange
            var matchId = 1;
            var soccerMarketItem = new SoccerMarketItem
            {
                IsSuccess = false,
                Data = new List<Market>()
            };
            _mockInvokeExternalApi.GetAsync<SoccerMarketItem>(Arg.Any<string>())!.Returns(Task.FromResult(soccerMarketItem));

            // Act
            var result = await _service.GetMarkets(matchId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateMarkets_ShouldCallHandleUpdateResultResponse()
        {
            // Arrange
            var request = new JTResultMarketRequestModel();
            var markets = new List<Market> { new Market() };
            var matchId = 1;
            var response = new HttpResponseMessage();
            _mockInvokeExternalApi.PutAsync(request, Arg.Any<string>()).Returns(Task.FromResult(response));

            // Act
            await _service.UpdateMarkets(request, markets, matchId);

            // Assert
            await _mockHandleResponseService.Received(1).HandleUpdateResultResponse(response, markets, matchId);
        }

        [Fact]
        public void CreateSoccerJTRequest_ShouldReturnJTResultMarketRequestModel()
        {
            // Arrange
            var markets = new List<Market> { new Market() };
            var results = new List<JackTimeResult> { new JackTimeResult() };

            // Act
            var result = _service.CreateSoccerJTRequest(markets, results);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Header);
            Assert.NotNull(result.Request);
        }

        [Fact]
        public async Task UpdateVoidBetTicket_ShouldCallHandleVoidBetResponse()
        {
            // Arrange
            var request = new VoidBetRequest
            {
                VoidBetInfos = new List<VoidBetInfo> { new VoidBetInfo { Action = "VOID" } }
            };
            var matchId = 1;
            var response = new HttpResponseMessage();
            _mockInvokeExternalApi.PostAsync(Arg.Any<VoidBetApiModel>(), Arg.Any<string>()).Returns(Task.FromResult(response));

            // Act
            await _service.UpdateVoidBetTicket(request, matchId);

            // Assert
            await _mockHandleResponseService.Received(1).HandleVoidBetResponse(response, Arg.Any<VoidBetApiModel>(), matchId);
        }
    }
}

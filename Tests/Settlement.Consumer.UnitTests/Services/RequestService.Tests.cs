using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using Microsoft.Extensions.Options;
using NSubstitute;
using Settlement.Consumer.Constants;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;
using Settlement.Shared.Models;
using Settlement.Shared.Services;
using SharpRaven;

namespace Settlement.Consumer.UnitTests.Services;

public class RequestServiceTests
{
    private readonly IInvokeExternalAPI api;
    private readonly IOptions<AppSettingsKey> options;
    private readonly IHandleResponseService handleResponseService;
    private readonly RequestService service;

    public RequestServiceTests()
    {
        var logEngine = new List<ILogEngine>();
        var sentryEngineOptions = new SentryEngineOptions
        {
            Dsn = new Dsn("http://2dfee6c5bbc844558ad3bef5f4b59ee1@sentry.nexdev.net/9")
        };
        logEngine.Add(new SentryLogging(sentryEngineOptions));
        LogManager.SetDefaultLogCategory("NAP-Settlement-LOCAL").Use(logEngine);
        api = Substitute.For<IInvokeExternalAPI>();
        options = Substitute.For<IOptions<AppSettingsKey>>();
        handleResponseService = Substitute.For<IHandleResponseService>();

        options.Value.Returns(new AppSettingsKey
        {
            ThirdPartyUri = new ThirdPartySettingKeys
            {
                JackTimeMarkets = "https://host"
            }
        });

        service = new RequestService(api, options, handleResponseService);
    }

    [Fact]
    public async Task GetMarkets_WhenApiSuccess_ReturnsData()
    {
        // Arrange
        var apiResponse = new SoccerMarketItem
        {
            IsSuccess = true,
            Data = new List<Market> { new() { BetType = 1 } }
        };

        api.GetAsync<SoccerMarketItem>("https://host/" + CashOutEndpoints.GetMarkets + "/?MatchIds=10")
            .Returns(apiResponse);

        // Act
        var result = await service.GetMarkets(10);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetMarkets_WhenApiFails_ReturnsEmpty()
    {
        // Arrange
        api.GetAsync<SoccerMarketItem>(Arg.Any<string>()).Returns((SoccerMarketItem)null);

        // Act
        var result = await service.GetMarkets(10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateMarkets_CallsApiAndHandlesResponse()
    {
        // Arrange
        var request = new JTResultMarketRequestModel
        {
            Markets = new List<MarketData>()
        };
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        api.PutAsync(request, "https://host/" + CashOutEndpoints.ResultMarkets)
            .Returns(response);

        // Act
        await service.UpdateMarkets(request, new List<Market>(), 5);

        // Assert
        await handleResponseService.Received(1)
            .HandleUpdateResultResponse(response, Arg.Any<IEnumerable<Market>>(), 5);
    }

    [Fact]
    public void CreateSoccerJTRequest_BuildsWayScoresByMatchingResults()
    {
        // Arrange
        var markets = new List<Market>
        {
            new() { BetType = 1, GoalLine = 0.5m },
            new() { BetType = 2, GoalLine = 1.0m }
        };
        var results = new List<JackTimeResult>
        {
            new() { Bettype = 1, Hdp = 0.5m, Way = 7, OutCome = 1 },
            new() { Bettype = 2, Hdp = 1.0m, Way = 9, OutCome = 0 }
        };

        // Act
        var req = service.CreateSoccerJTRequest(markets, results);

        // Assert
        Assert.Equal(2, req.Markets.Count());
        Assert.Contains(req.Markets, m => m.Market.BetType == 1 && m.WayScores.Any(ws => ws.Id == 7 && ws.Outcome == 1));
        Assert.Contains(req.Markets, m => m.Market.BetType == 2 && m.WayScores.Any(ws => ws.Id == 9 && ws.Outcome == 0));
    }
}




using AutoMapper;
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;
using Settlement.Shared.Models;

namespace Settlement.Consumer.UnitTests.Services;

public class SoccerJackTimeServiceTests
{
    private readonly IDynamicRepository repository;
    private readonly IMapper mapper;
    private readonly IRequestService requestService;
    private readonly SoccerJackTimeService service;

    public SoccerJackTimeServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        mapper = Substitute.For<IMapper>();
        requestService = Substitute.For<IRequestService>();
        service = new SoccerJackTimeService(repository, mapper, requestService);
    }

    [Fact]
    public async Task UpdateResultMarket_ShouldMapCriteria_FetchResults_AndUpdateMarkets()
    {
        // Arrange
        var result = new SoccerResult { MatchId = 123 };
        var marketOffered = new List<Market>
        {
            new() { BetType = 1, GoalLine = 0.5m }
        };

        var mappedCriteria = new CashOutSelCriteria { SelectedResources = "res" };
        mapper.Map<SoccerResult, CashOutSelCriteria>(result).Returns(mappedCriteria);

        var jackTimeResults = new List<JackTimeResult>
        {
            new() { Bettype = 1, Hdp = 0.5m, Way = 3, OutCome = 1 }
        };
        repository.FetchAsync<JackTimeResult>(Arg.Any<CashOutSelCriteria>())
            .Returns(jackTimeResults);

        var jtRequest = new Settlement.Consumer.Models.SoccerJackTime.JTResultMarketRequestModel();
        requestService.CreateSoccerJTRequest(marketOffered, jackTimeResults)
            .Returns(jtRequest);

        // Act
        await service.UpdateResultMarket(result, marketOffered);

        // Assert
        await repository.Received(1)
            .FetchAsync<JackTimeResult>(Arg.Is<CashOutSelCriteria>(c => c.Markets == Newtonsoft.Json.JsonConvert.SerializeObject(marketOffered)));

        await requestService.Received(1)
            .UpdateMarkets(jtRequest, marketOffered, result.MatchId);
    }
}




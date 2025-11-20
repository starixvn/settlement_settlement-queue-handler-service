using Fanex.Data.Repository;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.JackTimeHybrid;
using Settlement.Consumer.Services;
using Settlement.JackTime.Criteria;
using Settlement.JackTime.Models;

namespace Settlement.Consumer.UnitTests.Services;

public class OddsServiceTests
{
    private readonly IDynamicRepository mockRepository;
    private readonly OddsServiceImp service;

    public OddsServiceTests()
    {
        mockRepository = Substitute.For<IDynamicRepository>();
        service = new OddsServiceImp(mockRepository);
    }

    [Fact]
    public async Task GetOdds_WhenResultsFound_ShouldReturnOddsWithoutRetry()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1,2,3";
        var expectedOdds = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m),
            new(matchId, 3, 2.5m)
        };

        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns(expectedOdds);

        var result = await service.GetOdds(matchId, logDate, transIds);

        Assert.Equal(3, result.Count);
        Assert.Equal(expectedOdds[0], result[0]);
        Assert.Equal(expectedOdds[1], result[1]);
        Assert.Equal(expectedOdds[2], result[2]);

        await mockRepository.Received(1).FetchAsync<OddModel>(
            Arg.Is<HybridParlayLogSelCriteria>(c =>
                c.MatchId == matchId &&
                c.LogDate == logDate &&
                c.TransIds == transIds));
    }

    [Fact]
    public async Task GetOdds_WhenNoResultsFound_ShouldRetry()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1,2";
        var emptyResults = new List<OddModel>();
        var resultsWithAllData = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m)
        };

        var callCount = 0;
        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns(callInfo =>
            {
                callCount++;
                return callCount <= 2 ? emptyResults : resultsWithAllData;
            });

        var result = await service.GetOdds(matchId, logDate, transIds);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].TransId);
        Assert.Equal(2, result[1].TransId);
        Assert.True(callCount >= 3);
    }

    [Fact]
    public async Task GetOdds_WhenAlwaysEmpty_ShouldRetryMaxTimes()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1";
        var emptyResults = new List<OddModel>();

        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns(emptyResults);

        var result = await service.GetOdds(matchId, logDate, transIds);

        Assert.Empty(result);
        await mockRepository.Received(11).FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>());
    }

    [Fact]
    public async Task GetOdds_WhenPartialResults_ShouldRetryUntilComplete()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1,2,3";
        var partialResults = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m)
        };
        var completeResults = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m),
            new(matchId, 3, 2.5m)
        };

        var callCount = 0;
        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns(callInfo =>
            {
                callCount++;
                return callCount <= 2 ? partialResults : completeResults;
            });

        var result = await service.GetOdds(matchId, logDate, transIds);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].TransId);
        Assert.Equal(2, result[1].TransId);
        Assert.Equal(3, result[2].TransId);
        Assert.True(callCount >= 3);
    }

    [Fact]
    public async Task GetOdds_WhenPartialResultsNeverComplete_ShouldRetryMaxTimes()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1,2,3";
        var partialResults = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m)
        };

        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns(partialResults);

        var result = await service.GetOdds(matchId, logDate, transIds);

        Assert.Equal(2, result.Count);
        await mockRepository.Received(11).FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>());
    }

    [Fact]
    public async Task GetOdds_WhenNullReturned_ShouldThrowException()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1";

        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns((List<OddModel>?)null!);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await service.GetOdds(matchId, logDate, transIds));
    }

    [Fact]
    public async Task UpdateOdds_WhenSuccessful_ShouldReturnValidationTransIds()
    {
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m)
        };
        var siteName = "Server1";
        var userId = 456;
        var matchId = 123;
        var expectedResult = new ValidationTransIds
        {
            AffectedTransIds = "1,2",
            WrongOddsTransIds = string.Empty,
            InvalidTransIds = string.Empty
        };

        mockRepository.GetAsync<ValidationTransIds>(Arg.Any<UpdateLegOddsCriteria>())
            .Returns(expectedResult);

        var result = await service.UpdateOdds(odds, siteName, userId, matchId);

        Assert.Equal(expectedResult.AffectedTransIds, result.AffectedTransIds);
        Assert.Equal(expectedResult.WrongOddsTransIds, result.WrongOddsTransIds);
        Assert.Equal(expectedResult.InvalidTransIds, result.InvalidTransIds);

        await mockRepository.Received(1).GetAsync<ValidationTransIds>(
            Arg.Is<UpdateLegOddsCriteria>(c =>
                c.MatchId == matchId &&
                c.Creator == userId &&
                c.DbServer == siteName &&
                !string.IsNullOrEmpty(c.LegOddsJSON)));
    }

    [Fact]
    public async Task UpdateOdds_WhenHasWrongOddsTransIds_ShouldReturnValidationTransIds()
    {
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m)
        };
        var siteName = "Server1";
        var userId = 456;
        var matchId = 123;
        var expectedResult = new ValidationTransIds
        {
            AffectedTransIds = string.Empty,
            WrongOddsTransIds = "1",
            InvalidTransIds = string.Empty
        };

        mockRepository.GetAsync<ValidationTransIds>(Arg.Any<UpdateLegOddsCriteria>())
            .Returns(expectedResult);

        var result = await service.UpdateOdds(odds, siteName, userId, matchId);

        Assert.Equal(expectedResult.WrongOddsTransIds, result.WrongOddsTransIds);
        Assert.Empty(result.AffectedTransIds);
    }

    [Fact]
    public async Task UpdateOdds_ShouldSerializeOddsToJson()
    {
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m),
            new(456, 3, 3.5m)
        };
        var siteName = "Server1";
        var userId = 456;
        var matchId = 123;

        mockRepository.GetAsync<ValidationTransIds>(Arg.Any<UpdateLegOddsCriteria>())
            .Returns(new ValidationTransIds());

        await service.UpdateOdds(odds, siteName, userId, matchId);

        await mockRepository.Received(1).GetAsync<ValidationTransIds>(
            Arg.Is<UpdateLegOddsCriteria>(c =>
                c.LegOddsJSON.Contains("123") &&
                c.LegOddsJSON.Contains("456") &&
                c.LegOddsJSON.Contains("1") &&
                c.LegOddsJSON.Contains("2") &&
                c.LegOddsJSON.Contains("3") &&
                c.LegOddsJSON.Contains("1.5") &&
                c.LegOddsJSON.Contains("2.0") &&
                c.LegOddsJSON.Contains("3.5")));
    }

    [Fact]
    public async Task UpdateOdds_WhenEmptyOdds_ShouldStillCallGetAsync()
    {
        var odds = new List<OddModel>();
        var siteName = "Server1";
        var userId = 456;
        var matchId = 123;

        mockRepository.GetAsync<ValidationTransIds>(Arg.Any<UpdateLegOddsCriteria>())
            .Returns(new ValidationTransIds());

        await service.UpdateOdds(odds, siteName, userId, matchId);

        await mockRepository.Received(1).GetAsync<ValidationTransIds>(
            Arg.Is<UpdateLegOddsCriteria>(c =>
                c.MatchId == matchId &&
                c.Creator == userId &&
                c.DbServer == siteName &&
                !string.IsNullOrEmpty(c.LegOddsJSON)));
    }

    [Fact]
    public async Task UpdateOdds_ShouldSetCriteriaPropertiesCorrectly()
    {
        var odds = new List<OddModel>
        {
            new(789, 10, 5.5m)
        };
        var siteName = "TestServer";
        var userId = 999;
        var matchId = 789;

        mockRepository.GetAsync<ValidationTransIds>(Arg.Any<UpdateLegOddsCriteria>())
            .Returns(new ValidationTransIds());

        await service.UpdateOdds(odds, siteName, userId, matchId);

        await mockRepository.Received(1).GetAsync<ValidationTransIds>(
            Arg.Is<UpdateLegOddsCriteria>(c =>
                c.MatchId == matchId &&
                c.Creator == userId &&
                c.DbServer == siteName));
    }

    [Fact]
    public async Task UpdateOdds_WhenAffectedTransIdsProvided_ShouldReturnThem()
    {
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m),
            new(123, 3, 2.5m)
        };
        var siteName = "Server1";
        var userId = 456;
        var matchId = 123;
        var affectedTransIds = "1,3";
        var expectedResult = new ValidationTransIds
        {
            AffectedTransIds = affectedTransIds,
            WrongOddsTransIds = string.Empty,
            InvalidTransIds = string.Empty
        };

        mockRepository.GetAsync<ValidationTransIds>(Arg.Any<UpdateLegOddsCriteria>())
            .Returns(expectedResult);

        var result = await service.UpdateOdds(odds, siteName, userId, matchId);

        Assert.Equal(affectedTransIds, result.AffectedTransIds);
    }
}
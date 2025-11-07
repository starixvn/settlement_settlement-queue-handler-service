using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using Fanex.SiteDbConnectionManager.Client.Models;
using Fanex.SiteDbConnectionManager.Client.Services;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.JackTimeHybrid;
using Settlement.Consumer.Services;
using Settlement.JackTime.Criteria;

namespace Settlement.Consumer.UnitTests.Services;

public class JackTimeHybridServiceTests
{
    private readonly IDynamicRepository mockRepository;
    private readonly SiteDbClientService mockSiteDbClientService;
    private readonly JackTimeHybridService service;

    public JackTimeHybridServiceTests()
    {
        mockRepository = Substitute.For<IDynamicRepository>();
        mockSiteDbClientService = Substitute.For<SiteDbClientService>();
        service = new JackTimeHybridService(mockRepository, mockSiteDbClientService);
    }

    [Fact]
    public async Task ProcessHybridMatch_WhenIsUpdateOddSuccessfullyIsTrue_ShouldCalculateHybridMatch()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456,
            Action = "TestAction"
        };
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 },
            new() { SiteName = "Server2", SiteDbGroupId = 2 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        mockRepository.ExecuteAsync(Arg.Any<CalculateHybridMatchCriteria>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<CalculateHybridMatchCriteria>();
                criteria.ErrCode = 0;
            });

        await service.ProcessHybridMatch(hybridMatch, true);

        await mockSiteDbClientService.Received(1).GetActiveSiteDbAsync();
        await mockRepository.Received(2).ExecuteAsync(
            Arg.Is<CalculateHybridMatchCriteria>(c => 
                c.MatchId == hybridMatch.MatchId &&
                c.Creator == hybridMatch.UserId &&
                c.Action == hybridMatch.Action &&
                (c.DbServer == "Server1" || c.DbServer == "Server2")));
    }

    [Fact]
    public async Task ProcessHybridMatch_WhenIsUpdateOddSuccessfullyIsFalse_ShouldNotCalculateHybridMatch()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456,
            Action = "TestAction"
        };

        await service.ProcessHybridMatch(hybridMatch, false);

        await mockSiteDbClientService.DidNotReceive().GetActiveSiteDbAsync();
        await mockRepository.DidNotReceive().ExecuteAsync(Arg.Any<CalculateHybridMatchCriteria>());
    }

    [Fact]
    public async Task UpdateLegOdds_WhenAtLeastOneUpdateSucceeds_ShouldReturnTrue()
    {
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m)
        };
        var userId = 456;
        var matchId = 123;
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 },
            new() { SiteName = "Server2", SiteDbGroupId = 2 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        mockRepository.ExecuteAsync(Arg.Any<UpdateLegOddsCriteria>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<UpdateLegOddsCriteria>();
                if (criteria.DbServer == "Server1")
                {
                    criteria.ErrCode = 0;
                }
                else
                {
                    criteria.ErrCode = 1;
                }
            });

        var result = await service.UpdateLegOdds(odds, userId, matchId);

        Assert.True(result);
        await mockRepository.Received(2).ExecuteAsync(
            Arg.Is<UpdateLegOddsCriteria>(c => 
                c.MatchId == matchId &&
                c.Creator == userId &&
                !string.IsNullOrEmpty(c.LegOddsJSON)));
    }

    [Fact]
    public async Task UpdateLegOdds_WhenAllUpdatesFail_ShouldReturnFalse()
    {
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m)
        };
        var userId = 456;
        var matchId = 123;
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        mockRepository.ExecuteAsync(Arg.Any<UpdateLegOddsCriteria>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<UpdateLegOddsCriteria>();
                criteria.ErrCode = 1;
            });

        var result = await service.UpdateLegOdds(odds, userId, matchId);

        Assert.False(result);
    }

    [Fact]
    public async Task GetOdds_WhenAllTransIdsExist_ShouldReturnOddsAndEmptyNotExistList()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1,2,3";
        var odds = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m),
            new(matchId, 3, 2.5m)
        };

        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns(odds);

        var result = await service.GetOdds(matchId, logDate, transIds);

        Assert.Equal(3, result.odds.Count);
        Assert.Empty(result.notExistTransIds);
    }

    [Fact]
    public async Task GetOdds_WhenSomeTransIdsNotExist_ShouldReturnOddsAndNotExistTransIds()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1,2,3,4";
        var odds = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m)
        };

        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns(odds);

        var result = await service.GetOdds(matchId, logDate, transIds);

        Assert.Equal(2, result.odds.Count);
        Assert.Equal(2, result.notExistTransIds.Count);
        Assert.Contains("3", result.notExistTransIds);
        Assert.Contains("4", result.notExistTransIds);
    }

    [Fact]
    public async Task GetOdds_WhenNoOddsFound_ShouldReturnEmptyOddsAndAllTransIds()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1,2,3";
        var emptyOdds = new List<OddModel>();

        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns(emptyOdds);

        var result = await service.GetOdds(matchId, logDate, transIds);

        Assert.Empty(result.odds);
        Assert.Equal(3, result.notExistTransIds.Count);
        Assert.Contains("1", result.notExistTransIds);
        Assert.Contains("2", result.notExistTransIds);
        Assert.Contains("3", result.notExistTransIds);
    }

    [Fact]
    public async Task GetOdds_WhenRepositoryReturnsNull_ShouldReturnEmptyOddsAndAllTransIds()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var transIds = "1,2";

        mockRepository.FetchAsync<OddModel>(Arg.Any<HybridParlayLogSelCriteria>())
            .Returns((List<OddModel>?)null!);

        var result = await service.GetOdds(matchId, logDate, transIds);

        Assert.Empty(result.odds);
        Assert.Equal(2, result.notExistTransIds.Count);
    }

    [Fact]
    public async Task UpdateLegOdds_ShouldSerializeOddsToJson()
    {
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m)
        };
        var userId = 456;
        var matchId = 123;
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        mockRepository.ExecuteAsync(Arg.Any<UpdateLegOddsCriteria>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<UpdateLegOddsCriteria>();
                criteria.ErrCode = 0;
            });

        await service.UpdateLegOdds(odds, userId, matchId);

        await mockRepository.Received().ExecuteAsync(
            Arg.Is<UpdateLegOddsCriteria>(c => 
                !string.IsNullOrEmpty(c.LegOddsJSON) &&
                c.LegOddsJSON.Contains("123") &&
                c.LegOddsJSON.Contains("1.5") &&
                c.LegOddsJSON.Contains("2.0")));
    }
}


using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using Fanex.SiteDbConnectionManager.Client.Models;
using Fanex.SiteDbConnectionManager.Client.Services;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.JackTimeHybrid;
using Settlement.Consumer.Services;
using Settlement.JackTime.Criteria;
using Settlement.JackTime.Models;
using Settlement.Shared.Models;
using Xunit;

namespace Settlement.Consumer.UnitTests.Services;

public class JackTimeHybridServiceTests
{
    private readonly IDynamicRepository mockRepository;
    private readonly SiteDbClientService mockSiteDbClientService;
    private readonly OddsService mockOddsService;
    private readonly HybridParlayLogService mockHybridLogService;
    private readonly HybridParlayServiceImp service;

    public JackTimeHybridServiceTests()
    {
        mockRepository = Substitute.For<IDynamicRepository>();
        mockSiteDbClientService = Substitute.For<SiteDbClientService>();
        mockOddsService = Substitute.For<OddsService>();
        mockHybridLogService = Substitute.For<HybridParlayLogService>();
        service = new HybridParlayServiceImp(mockRepository, mockSiteDbClientService, mockOddsService, mockHybridLogService);
    }

    [Fact]
    public async Task GetOddsAndInvalidTransId_WhenAllTransIdsExist_ShouldReturnOddsAndEmptyInvalidList()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var originalTransId = new List<TranInfo>
        {
            new() { TransId = "1" },
            new() { TransId = "2" },
            new() { TransId = "3" }
        };
        var odds = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m),
            new(matchId, 3, 2.5m)
        };

        mockOddsService.GetOdds(matchId, logDate, "1,2,3")
            .Returns(odds);

        var result = await service.GetOddsAndInvalidTransId(matchId, logDate, originalTransId);

        Assert.Equal(3, result.Odds.Count);
        Assert.Empty(result.InvalidTransId);
    }

    [Fact]
    public async Task GetOddsAndInvalidTransId_WhenSomeTransIdsNotExist_ShouldReturnOddsAndInvalidTransIds()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var originalTransId = new List<TranInfo>
        {
            new() { TransId = "1" },
            new() { TransId = "2" },
            new() { TransId = "3" },
            new() { TransId = "4" }
        };
        var odds = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m)
        };

        mockOddsService.GetOdds(matchId, logDate, "1,2,3,4")
            .Returns(odds);

        var result = await service.GetOddsAndInvalidTransId(matchId, logDate, originalTransId);

        Assert.Equal(2, result.Odds.Count);
        Assert.Equal(2, result.InvalidTransId.Count);
        Assert.Contains("3", result.InvalidTransId);
        Assert.Contains("4", result.InvalidTransId);
    }

    [Fact]
    public async Task GetOddsAndInvalidTransId_WhenNoOddsFound_ShouldReturnEmptyOddsAndAllTransIds()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var originalTransId = new List<TranInfo>
        {
            new() { TransId = "1" },
            new() { TransId = "2" },
            new() { TransId = "3" }
        };
        var emptyOdds = new List<OddModel>();

        mockOddsService.GetOdds(matchId, logDate, "1,2,3")
            .Returns(emptyOdds);

        var result = await service.GetOddsAndInvalidTransId(matchId, logDate, originalTransId);

        Assert.Empty(result.Odds);
        Assert.Equal(3, result.InvalidTransId.Count);
        Assert.Contains("1", result.InvalidTransId);
        Assert.Contains("2", result.InvalidTransId);
        Assert.Contains("3", result.InvalidTransId);
    }

    [Fact]
    public async Task GetOddsAndInvalidTransId_WhenTransIdsHaveSpaces_ShouldTrimAndProcessCorrectly()
    {
        var matchId = 123;
        var logDate = DateTimeOffset.Now;
        var originalTransId = new List<TranInfo>
        {
            new() { TransId = "1" },
            new() { TransId = "2" },
            new() { TransId = "3" }
        };
        var odds = new List<OddModel>
        {
            new(matchId, 1, 1.5m),
            new(matchId, 2, 2.0m)
        };

        mockOddsService.GetOdds(matchId, logDate, "1,2,3")
            .Returns(odds);

        var result = await service.GetOddsAndInvalidTransId(matchId, logDate, originalTransId);

        Assert.Equal(2, result.Odds.Count);
        Assert.Single(result.InvalidTransId);
        Assert.Contains("3", result.InvalidTransId);
    }

    [Fact]
    public async Task UpdateOddsAndSettle_WhenUpdateSuccessfulAndAffectedTransIdsEmpty_ShouldReturnAllTransIds()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456,
            Action = "TestAction"
        };
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m)
        };
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        var validationTransIds = new ValidationTransIds
        {
            AffectedTransIds = string.Empty,
            WrongOddsTransIds = "1,2",
            InvalidTransIds = string.Empty
        };
        mockOddsService.UpdateOdds(odds, "Server1", hybridMatch.UserId, hybridMatch.MatchId)
            .Returns(validationTransIds);

        var originalTransId = new List<TranInfo>();

        await service.UpdateOddsAndSettle(hybridMatch, odds, originalTransId);

        await mockHybridLogService.Received(1).InvalidTransIdsUpdateOdds(
            hybridMatch,
            Arg.Is<List<ValidationTransIds>>(list => list.Count == 1 && list[0].WrongOddsTransIds == "1,2"),
            Arg.Any<List<TranInfo>>());
    }

    [Fact]
    public async Task UpdateOddsAndSettle_WhenUpdateSuccessfulWithAffectedTransIds_ShouldCalculateAndReturnInvalidTransIds()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456,
            Action = "TestAction"
        };
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m),
            new(123, 3, 2.5m)
        };
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        var validationTransIds = new ValidationTransIds
        {
            AffectedTransIds = "1,2",
            WrongOddsTransIds = "3",
            InvalidTransIds = string.Empty
        };
        mockOddsService.UpdateOdds(odds, "Server1", hybridMatch.UserId, hybridMatch.MatchId)
            .Returns(validationTransIds);

        var originalTransId = new List<TranInfo>();

        await service.UpdateOddsAndSettle(hybridMatch, odds, originalTransId);

        await mockRepository.Received(1).ExecuteAsync(
            Arg.Is<CalculateHybridMatchCriteria>(c =>
                c.MatchId == hybridMatch.MatchId &&
                c.Creator == hybridMatch.UserId &&
                c.Action == hybridMatch.Action &&
                c.TransIds == "1,2" &&
                c.DbServer == "Server1"));

        await mockHybridLogService.Received(1).InvalidTransIdsUpdateOdds(
            hybridMatch,
            Arg.Is<List<ValidationTransIds>>(list => list.Count == 1 && list[0].WrongOddsTransIds == "3"),
            Arg.Any<List<TranInfo>>());
    }

    [Fact]
    public async Task UpdateOddsAndSettle_WhenUpdateSuccessfulWithAllAffectedTransIds_ShouldNotCallInvalidLegOdds()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456,
            Action = "TestAction"
        };
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m)
        };
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        var validationTransIds = new ValidationTransIds
        {
            AffectedTransIds = "1,2",
            WrongOddsTransIds = string.Empty,
            InvalidTransIds = string.Empty
        };
        mockOddsService.UpdateOdds(odds, "Server1", hybridMatch.UserId, hybridMatch.MatchId)
            .Returns(validationTransIds);

        var originalTransId = new List<TranInfo>();

        await service.UpdateOddsAndSettle(hybridMatch, odds, originalTransId);

        await mockRepository.Received(1).ExecuteAsync(Arg.Any<CalculateHybridMatchCriteria>());
        await mockHybridLogService.DidNotReceive().InvalidTransIdsUpdateOdds(Arg.Any<HybridMatch>(), Arg.Any<List<ValidationTransIds>>(), Arg.Any<List<TranInfo>>());
    }

    [Fact]
    public async Task UpdateOddsAndSettle_WhenUpdateFails_ShouldReturnAllTransIds()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456,
            Action = "TestAction"
        };
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m)
        };
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        var validationTransIds = new ValidationTransIds
        {
            AffectedTransIds = string.Empty,
            WrongOddsTransIds = "1,2",
            InvalidTransIds = string.Empty
        };
        mockOddsService.UpdateOdds(odds, "Server1", hybridMatch.UserId, hybridMatch.MatchId)
            .Returns(validationTransIds);

        var originalTransId = new List<TranInfo>();

        await service.UpdateOddsAndSettle(hybridMatch, odds, originalTransId);

        await mockRepository.DidNotReceive().ExecuteAsync(Arg.Any<CalculateHybridMatchCriteria>());
        await mockHybridLogService.Received(1).InvalidTransIdsUpdateOdds(
            hybridMatch,
            Arg.Is<List<ValidationTransIds>>(list => list.Count == 1 && list[0].WrongOddsTransIds == "1,2"),
            Arg.Any<List<TranInfo>>());
    }

    [Fact]
    public async Task UpdateOddsAndSettle_WhenMultipleSiteDbs_ShouldProcessAllAndCallInvalidLegOddsOnce()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456,
            Action = "TestAction"
        };
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m)
        };
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 },
            new() { SiteName = "Server2", SiteDbGroupId = 2 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        var validationTransIds = new ValidationTransIds
        {
            AffectedTransIds = string.Empty,
            WrongOddsTransIds = "1",
            InvalidTransIds = string.Empty
        };
        mockOddsService.UpdateOdds(odds, Arg.Any<string>(), hybridMatch.UserId, hybridMatch.MatchId)
            .Returns(validationTransIds);

        var originalTransId = new List<TranInfo>();

        await service.UpdateOddsAndSettle(hybridMatch, odds, originalTransId);

        await mockOddsService.Received(2).UpdateOdds(odds, Arg.Any<string>(), hybridMatch.UserId, hybridMatch.MatchId);
        await mockHybridLogService.Received(1).InvalidTransIdsUpdateOdds(
            hybridMatch,
            Arg.Is<List<ValidationTransIds>>(list => list.Count == 2 && list.All(v => v.WrongOddsTransIds == "1")),
            Arg.Any<List<TranInfo>>());
    }

    [Fact]
    public async Task UpdateOddsAndSettle_WhenInvalidTransIds_ShouldCallInvalidTransIdsSendLetstalk()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456,
            Action = "TestAction"
        };
        var odds = new List<OddModel>
        {
            new(123, 1, 1.5m),
            new(123, 2, 2.0m)
        };
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        var validationTransIds = new ValidationTransIds
        {
            AffectedTransIds = "1",
            WrongOddsTransIds = string.Empty,
            InvalidTransIds = "2"
        };
        mockOddsService.UpdateOdds(odds, "Server1", hybridMatch.UserId, hybridMatch.MatchId)
            .Returns(validationTransIds);

        var originalTransId = new List<TranInfo>();

        await service.UpdateOddsAndSettle(hybridMatch, odds, originalTransId);

        await mockRepository.Received(1).ExecuteAsync(
            Arg.Is<CalculateHybridMatchCriteria>(c =>
                c.MatchId == hybridMatch.MatchId &&
                c.Creator == hybridMatch.UserId &&
                c.Action == hybridMatch.Action &&
                c.TransIds == "1" &&
                c.DbServer == "Server1"));

        await mockHybridLogService.Received(1).InvalidTransIdsUpdateOdds(
            hybridMatch,
            Arg.Is<List<ValidationTransIds>>(list =>
                list.Count == 1 &&
                list[0].InvalidTransIds == "2" &&
                string.IsNullOrEmpty(list[0].WrongOddsTransIds)),
            Arg.Any<List<TranInfo>>());
    }

    [Fact]
    public async Task GetHyBridTransId_ShouldReturnCommaSeparatedTransIds()
    {
        var matchId = 123;
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        var tranInfos = new List<TranInfo>
        {
            new() { TransId = "1" },
            new() { TransId = "2" },
            new() { TransId = "3" }
        };

        mockRepository.FetchAsync<TranInfo>(Arg.Any<HyBridValidateCriteria>())
            .Returns(tranInfos)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<HyBridValidateCriteria>();
                criteria.ErrCode = 0;
            });

        var result = await service.GetHyBridTransId(matchId, string.Empty);

        Assert.Equal(3, result.Count);
        Assert.Equal("1", result[0].TransId);
        Assert.Equal("2", result[1].TransId);
        Assert.Equal("3", result[2].TransId);
    }
}
using Fanex.Caching;
using Fanex.Data.Repository;
using Fanex.SiteDbConnectionManager.Client.Models;
using Fanex.SiteDbConnectionManager.Client.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Settlement.JackTime.Criteria;
using Settlement.JackTime.Models;
using Settlement.JackTime.Services;
using Settlement.Shared.Models;
using Xunit;

namespace Settlement.JackTime.UnitTests.Services;

public class JackTimeServiceTests
{
    private readonly ISoccerJackTimeService mockSoccerJackTimeService;
    private readonly IDynamicRepository mockRepository;
    private readonly ICacheService mockCache;
    private readonly SiteDbClientService mockSiteDbClientService;
    private readonly IOptions<AppSettingsKey> mockAppSettingsKey;
    private readonly JackTimeService jackTimeService;

    public JackTimeServiceTests()
    {
        mockSoccerJackTimeService = Substitute.For<ISoccerJackTimeService>();
        mockRepository = Substitute.For<IDynamicRepository>();
        mockCache = Substitute.For<ICacheService>();
        mockSiteDbClientService = Substitute.For<SiteDbClientService>();
        mockAppSettingsKey = Substitute.For<IOptions<AppSettingsKey>>();

        mockAppSettingsKey.Value.Returns(new AppSettingsKey
        {
            EnvironmentName = "LOCAL"
        });

        jackTimeService = new JackTimeService(
            mockSoccerJackTimeService,
            mockRepository,
            mockCache,
            mockSiteDbClientService,
            mockAppSettingsKey);
    }

    [Fact]
    public async Task GetMatches_WhenNoMatchesFromRepository_ShouldReturnEmptyList()
    {
        // Arrange
        SetupEmptyRepositoryResponse();

        // Act
        var result = await jackTimeService.GetMatches();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMatches_WhenMatchesExist_ShouldReturnGroupedMatches()
    {
        // Arrange
        var voidBetInfos = new List<VoidBetInfo>
        {
            new() { MatchId = 1, LeagueId = 100, TransId = "T1", CustId = "C1", Action = "VOID" },
            new() { MatchId = 1, LeagueId = 100, TransId = "T2", CustId = "C2", Action = "UNDO" },
            new() { MatchId = 2, LeagueId = 101, TransId = "T3", CustId = "C3", Action = "VOID" }
        };

        SetupRepositoryResponse(voidBetInfos);

        // Act
        var result = await jackTimeService.GetMatches();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.MatchId == 1 && m.LeagueId == 100);
        result.Should().Contain(m => m.MatchId == 2 && m.LeagueId == 101);
        result.First(m => m.MatchId == 1).VoidBetInfos.Should().HaveCount(2);
        result.First(m => m.MatchId == 2).VoidBetInfos.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMatches_WhenEnvironmentIsPRO_AndLeagueIdExists_ShouldFilterOutLeagueId()
    {
        // Arrange
        mockAppSettingsKey.Value.Returns(new AppSettingsKey
        {
            EnvironmentName = "PRO"
        });

        var voidBetInfos = new List<VoidBetInfo>
        {
            new() { MatchId = 1, LeagueId = 100, TransId = "T1", CustId = "C1", Action = "VOID" },
            new() { MatchId = 2, LeagueId = 200, TransId = "T2", CustId = "C2", Action = "VOID" }
        };

        SetupRepositoryResponse(voidBetInfos);
        mockCache.TryGetAsync<int>(Arg.Any<string>())
            .Returns((true, 100));

        // Act
        var result = await jackTimeService.GetMatches();

        // Assert
        result.Should().HaveCount(1);
        result.First().LeagueId.Should().Be(200);
    }

    [Fact]
    public async Task GetMatches_WhenEnvironmentIsPRO_AndLeagueIdIsZero_ShouldReturnAllMatches()
    {
        // Arrange
        mockAppSettingsKey.Value.Returns(new AppSettingsKey
        {
            EnvironmentName = "PRO"
        });

        var voidBetInfos = new List<VoidBetInfo>
        {
            new() { MatchId = 1, LeagueId = 100, TransId = "T1", CustId = "C1", Action = "VOID" },
            new() { MatchId = 2, LeagueId = 200, TransId = "T2", CustId = "C2", Action = "VOID" }
        };

        SetupRepositoryResponse(voidBetInfos);
        mockCache.TryGetAsync<int>(Arg.Any<string>())
            .Returns((true, 0));

        // Act
        var result = await jackTimeService.GetMatches();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMatches_WhenEnvironmentIsUAT_ShouldReturnOnlyMatchingLeagueId()
    {
        // Arrange
        mockAppSettingsKey.Value.Returns(new AppSettingsKey
        {
            EnvironmentName = "UAT"
        });

        var voidBetInfos = new List<VoidBetInfo>
        {
            new() { MatchId = 1, LeagueId = 100, TransId = "T1", CustId = "C1", Action = "VOID" },
            new() { MatchId = 2, LeagueId = 200, TransId = "T2", CustId = "C2", Action = "VOID" }
        };

        SetupRepositoryResponse(voidBetInfos);
        mockCache.TryGetAsync<int>(Arg.Any<string>())
            .Returns((true, 100));

        // Act
        var result = await jackTimeService.GetMatches();

        // Assert
        result.Should().HaveCount(1);
        result.First().LeagueId.Should().Be(100);
    }

    [Fact]
    public async Task Consume_WhenSuccessful_ShouldCallSoccerJackTimeService()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            LeagueId = 100,
            VoidBetInfos = new List<VoidBetInfo>
            {
                new() { TransId = "T1", CustId = "C1", Action = "VOID" }
            }
        };

        // Act
        await jackTimeService.Consume(match);

        // Assert
        await mockSoccerJackTimeService.Received(1).UpdateTicket(match);
    }

    [Fact]
    public async Task Consume_WhenExceptionOccurs_ShouldNotThrow()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            LeagueId = 100,
            VoidBetInfos = new List<VoidBetInfo>()
        };

        mockSoccerJackTimeService.UpdateTicket(match)
            .Throws(new Exception("Test exception"));

        // Act & Assert
        var action = () => jackTimeService.Consume(match);
        await action.Should().NotThrowAsync();
    }

    private void SetupEmptyRepositoryResponse()
    {
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 },
            new() { SiteName = "Server2", SiteDbGroupId = 2 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        mockRepository.FetchAsync<VoidBetInfo>(Arg.Any<CashOutTicketSelCriteria>())
            .Returns(new List<VoidBetInfo>());
    }

    private void SetupRepositoryResponse(List<VoidBetInfo> voidBetInfos)
    {
        var siteDbServers = new List<SiteDb>
        {
            new() { SiteName = "Server1", SiteDbGroupId = 1 }
        };

        mockSiteDbClientService.GetActiveSiteDbAsync()
            .Returns(siteDbServers);

        mockRepository.FetchAsync<VoidBetInfo>(Arg.Any<CashOutTicketSelCriteria>())
            .Returns(voidBetInfos);
    }
}
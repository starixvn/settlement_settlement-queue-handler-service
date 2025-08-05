using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Settlement.JackTime.Models;
using Settlement.JackTime.Services;
using SharpRaven;
using Xunit;

namespace Settlement.JackTime.UnitTests.Services;

public class JackTimeControllerTests
{
    private readonly IJackTimeService mockJackTimeService;
    private readonly JackTimeController jackTimeController;

    public JackTimeControllerTests()
    {
        var logEngine = new List<ILogEngine>();

        var sentryEngineOptions = new SentryEngineOptions
        {
            Dsn = new Dsn("http://2dfee6c5bbc844558ad3bef5f4b59ee1@sentry.nexdev.net/9")
        };
        logEngine.Add(new SentryLogging(sentryEngineOptions));

        LogManager.SetDefaultLogCategory("NAP-Settlement-LOCAL").Use(logEngine);

        mockJackTimeService = Substitute.For<IJackTimeService>();
        jackTimeController = new JackTimeController(mockJackTimeService);
    }

    [Fact]
    public async Task ProcessAsync_WhenNoNewMatches_ShouldReturnEarly()
    {
        // Arrange
        mockJackTimeService.GetMatches().Returns(new List<Match>());

        // Act
        await jackTimeController.ProcessAsync();

        // Assert
        await mockJackTimeService.Received(1).GetMatches();
        await mockJackTimeService.DidNotReceive().Consume(Arg.Any<Match>());
    }

    [Fact]
    public async Task ProcessAsync_WhenNewMatchesExist_ShouldConsumeAllMatches()
    {
        // Arrange
        var matches = new List<Match>
        {
            new() { MatchId = 1, LeagueId = 100 },
            new() { MatchId = 2, LeagueId = 101 },
            new() { MatchId = 3, LeagueId = 102 }
        };

        mockJackTimeService.GetMatches().Returns(matches);

        // Act
        await jackTimeController.ProcessAsync();

        // Assert
        await mockJackTimeService.Received(1).GetMatches();
        await mockJackTimeService.Received(3).Consume(Arg.Any<Match>());

        foreach (var match in matches)
        {
            await mockJackTimeService.Received(1).Consume(match);
        }
    }

    [Fact]
    public async Task ProcessAsync_WhenGetMatchesThrowsException_ShouldNotThrow()
    {
        // Arrange
        mockJackTimeService.GetMatches().Throws(new Exception("Test exception"));

        // Act & Assert
        var action = () => jackTimeController.ProcessAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessAsync_WhenConsumeThrowsException_ShouldNotThrow()
    {
        // Arrange
        var matches = new List<Match>
        {
            new() { MatchId = 1, LeagueId = 100 },
            new() { MatchId = 2, LeagueId = 101 }
        };

        mockJackTimeService.GetMatches().Returns(matches);
        mockJackTimeService.Consume(Arg.Is<Match>(m => m.MatchId == 1))
            .Throws(new Exception("Consume exception"));

        // Act & Assert
        var action = () => jackTimeController.ProcessAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessAsync_WhenMultipleMatches_ShouldProcessAllMatchesInParallel()
    {
        // Arrange
        var matches = new List<Match>
        {
            new() { MatchId = 1, LeagueId = 100 },
            new() { MatchId = 2, LeagueId = 101 },
            new() { MatchId = 3, LeagueId = 102 }
        };

        mockJackTimeService.GetMatches().Returns(matches);

        var consumeCallCount = 0;
        mockJackTimeService.Consume(Arg.Any<Match>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => consumeCallCount++);

        // Act
        await jackTimeController.ProcessAsync();

        // Assert
        consumeCallCount.Should().Be(3);
        await mockJackTimeService.Received(3).Consume(Arg.Any<Match>());
    }

    [Fact]
    public async Task ProcessAsync_WhenSingleMatch_ShouldProcessMatch()
    {
        // Arrange
        var match = new Match { MatchId = 1, LeagueId = 100 };
        var matches = new List<Match> { match };

        mockJackTimeService.GetMatches().Returns(matches);

        // Act
        await jackTimeController.ProcessAsync();

        // Assert
        await mockJackTimeService.Received(1).GetMatches();
        await mockJackTimeService.Received(1).Consume(match);
    }
}
using FluentAssertions;
using NSubstitute;
using Settlement.JackTime.Models;
using Settlement.JackTime.Services;
using Xunit;

namespace Settlement.JackTime.UnitTests.Services;

public class PeekMatchServiceTests
{
    private readonly IJackTimeService mockJackTimeService;
    private readonly PeekMatchService peekMatchService;

    public PeekMatchServiceTests()
    {
        mockJackTimeService = Substitute.For<IJackTimeService>();
        peekMatchService = new PeekMatchService(mockJackTimeService);
    }

    [Fact]
    public async Task PeekNewMatch_WhenNoMatchesReturned_ShouldReturnEmptyList()
    {
        // Arrange
        mockJackTimeService.GetMatches().Returns(new List<Match>());

        // Act
        var result = await peekMatchService.PeekNewMatch();

        // Assert
        result.Should().BeEmpty();
        await mockJackTimeService.Received(1).GetMatches();
    }

    [Fact]
    public async Task PeekNewMatch_WhenNewMatchesReturned_ShouldReturnNewMatches()
    {
        // Arrange
        var newMatches = new List<Match>
        {
            new() { MatchId = 1, LeagueId = 100 },
            new() { MatchId = 2, LeagueId = 101 }
        };

        mockJackTimeService.GetMatches().Returns(newMatches);

        // Act
        var result = await peekMatchService.PeekNewMatch();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(newMatches);
        await mockJackTimeService.Received(1).GetMatches();
    }

    [Fact]
    public async Task PeekNewMatch_WhenDuplicateMatchesReturned_ShouldReturnOnlyNewMatches()
    {
        // Arrange
        var existingMatch = new Match { MatchId = 1, LeagueId = 100 };
        var newMatch = new Match { MatchId = 2, LeagueId = 101 };

        var allMatches = new List<Match> { existingMatch, newMatch };

        // First call to populate the pool
        mockJackTimeService.GetMatches().Returns(new List<Match> { existingMatch }, allMatches);

        // Act
        await peekMatchService.PeekNewMatch(); // First call to populate pool
        var result = await peekMatchService.PeekNewMatch(); // Second call

        // Assert
        result.Should().HaveCount(1);
        result.First().MatchId.Should().Be(2);
        await mockJackTimeService.Received(2).GetMatches();
    }

    [Fact]
    public async Task GetPoolOfMatches_ShouldReturnCopyOfPool()
    {
        // Arrange
        var matches = new List<Match>
        {
            new() { MatchId = 1, LeagueId = 100 },
            new() { MatchId = 2, LeagueId = 101 }
        };

        mockJackTimeService.GetMatches().Returns(matches);

        // Act
        await peekMatchService.PeekNewMatch(); // Populate pool
        var result = peekMatchService.GetPoolOfMatches();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(matches);
        result.Should().NotBeSameAs(matches); // Should be a copy
    }

    [Fact]
    public async Task Remove_ShouldRemoveMatchFromPool()
    {
        // Arrange
        var matchToRemove = new Match { MatchId = 1, LeagueId = 100 };
        var matches = new List<Match>
        {
            matchToRemove,
            new() { MatchId = 2, LeagueId = 101 }
        };

        mockJackTimeService.GetMatches().Returns(matches);

        // Act
        await peekMatchService.PeekNewMatch(); // Populate pool
        peekMatchService.Remove(matchToRemove);
        var result = peekMatchService.GetPoolOfMatches();

        // Assert
        result.Should().HaveCount(1);
        result.First().MatchId.Should().Be(2);
    }

    [Fact]
    public void Remove_WhenMatchNotInPool_ShouldNotThrowException()
    {
        // Arrange
        var nonExistentMatch = new Match { MatchId = 999, LeagueId = 999 };

        // Act & Assert
        var action = () => peekMatchService.Remove(nonExistentMatch);
        action.Should().NotThrow();
    }

    [Fact]
    public async Task PeekNewMatch_ShouldBeThreadSafe()
    {
        // Arrange
        var matches = new List<Match>
        {
            new() { MatchId = 1, LeagueId = 100 },
            new() { MatchId = 2, LeagueId = 101 }
        };

        mockJackTimeService.GetMatches().Returns(matches);

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => peekMatchService.PeekNewMatch())
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        await mockJackTimeService.Received(5).GetMatches();
    }
}
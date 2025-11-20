using NSubstitute;
using Settlement.JackTime.Models;
using Settlement.JackTime.Services.JackTime;
using Settlement.Shared.Models;
using Xunit;

namespace Settlement.JackTime.UnitTests.Services;

public class SoccerJackTimeServiceTests
{
    private readonly IRequestService mockRequestService;
    private readonly SoccerJackTimeService soccerJackTimeService;

    public SoccerJackTimeServiceTests()
    {
        mockRequestService = Substitute.For<IRequestService>();
        soccerJackTimeService = new SoccerJackTimeService(mockRequestService);
    }

    [Fact]
    public async Task UpdateTicket_WhenVoidBetInfosIsEmpty_ShouldNotCallRequestService()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            LeagueId = 100,
            VoidBetInfos = new List<VoidBetInfo>()
        };

        // Act
        await soccerJackTimeService.UpdateTicket(match);

        // Assert
        await mockRequestService.DidNotReceive().UpdateVoidBetTicket(Arg.Any<IList<VoidBetInfo>>(), Arg.Any<int>());
    }

    [Fact]
    public async Task UpdateTicket_WhenNoValidTicketInfos_ShouldNotCallRequestService()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            LeagueId = 100,
            VoidBetInfos = new List<VoidBetInfo>
            {
                new() { TransId = "T1", CustId = "C1", Action = "VOID", ExtraInfo = "invalid info" },
                new() { TransId = "T2", CustId = "C2", Action = "UNDO", ExtraInfo = "" }
            }
        };

        // Act
        await soccerJackTimeService.UpdateTicket(match);

        // Assert
        await mockRequestService.DidNotReceive().UpdateVoidBetTicket(Arg.Any<IList<VoidBetInfo>>(), Arg.Any<int>());
    }

    [Fact]
    public async Task UpdateTicket_WhenValidTicketInfosExist_ShouldCallRequestService()
    {
        // Arrange
        var validVoidBetInfo = new VoidBetInfo
        {
            TransId = "T1",
            CustId = "C1",
            Action = "VOID",
            ExtraInfo = "jtseqid=123"
        };

        var match = new Match
        {
            MatchId = 1,
            LeagueId = 100,
            VoidBetInfos = new List<VoidBetInfo>
            {
                validVoidBetInfo,
                new() { TransId = "T2", CustId = "C2", Action = "UNDO", ExtraInfo = "invalid info" }
            }
        };

        // Act
        await soccerJackTimeService.UpdateTicket(match);

        // Assert
        await mockRequestService.Received(1).UpdateVoidBetTicket(
            Arg.Is<IList<VoidBetInfo>>(list => list.Count == 1 && list.First().TransId == "T1"),
            match.MatchId);
    }

    [Fact]
    public async Task UpdateTicket_WhenMultipleValidTicketInfosExist_ShouldCallRequestServiceWithAllValid()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            LeagueId = 100,
            VoidBetInfos = new List<VoidBetInfo>
            {
                new() { TransId = "T1", CustId = "C1", Action = "VOID", ExtraInfo = "jtseqid=123" },
                new() { TransId = "T2", CustId = "C2", Action = "UNDO", ExtraInfo = "JTSEQID=456" },
                new() { TransId = "T3", CustId = "C3", Action = "VOID", ExtraInfo = "invalid info" }
            }
        };

        // Act
        await soccerJackTimeService.UpdateTicket(match);

        // Assert
        await mockRequestService.Received(1).UpdateVoidBetTicket(
            Arg.Is<IList<VoidBetInfo>>(list => list.Count == 2), match.MatchId);
    }

    [Theory]
    [InlineData("jtseqid=123", true)]
    [InlineData("JTSEQID=456", true)]
    [InlineData("JtSeQiD=789", true)]
    [InlineData("invalid info", false)]
    [InlineData("", false)]
    public async Task FilterValidTicketInfos_ShouldFilterCorrectly(string extraInfo, bool expected)
    {
        // Arrange
        var voidBetInfo = new VoidBetInfo
        {
            TransId = "T1",
            CustId = "C1",
            Action = "VOID",
            ExtraInfo = extraInfo
        };

        var match = new Match
        {
            MatchId = 1,
            LeagueId = 100,
            VoidBetInfos = new List<VoidBetInfo> { voidBetInfo }
        };

        // Act
        await soccerJackTimeService.UpdateTicket(match);

        // Assert
        if (expected)
        {
            await mockRequestService.Received(1).UpdateVoidBetTicket(
                Arg.Is<IList<VoidBetInfo>>(list => list.Count == 1), match.MatchId);
        }
        else
        {
            await mockRequestService.DidNotReceive().UpdateVoidBetTicket(Arg.Any<IList<VoidBetInfo>>(), Arg.Any<int>());
        }
    }
}
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models.JackTimeHybrid;
using Settlement.Consumer.Services;
using Settlement.JackTime.Models;
using Settlement.Shared.Services;
using Xunit;

namespace Settlement.Consumer.UnitTests.Services;

public class HybridLogServiceTests
{
    private readonly ILetsTalkService mockLetsTalkService;
    private readonly IDynamicRepository mockRepository;
    private readonly HybridParlayLogServiceImp service;

    public HybridLogServiceTests()
    {
        mockLetsTalkService = Substitute.For<ILetsTalkService>();
        mockRepository = Substitute.For<IDynamicRepository>();
        service = new HybridParlayLogServiceImp(mockLetsTalkService, mockRepository);
    }

    [Fact]
    public async Task InvalidTransId_WhenTransIdsNotEmpty_ShouldWriteLogAndSendMessage()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456
        };
        var transIds = new List<string> { "T1", "T2", "T3" };
        var originalTransId = new List<TranInfo>
        {
            new() { TransId = "T1", IsCredit = false },
            new() { TransId = "T2", IsCredit = false },
            new() { TransId = "T3", IsCredit = true }
        };

        await service.InvalidTransIdsFetchNewOdds(hybridMatch, transIds, originalTransId);

        await mockRepository.Received(1).ExecuteAsync(
            Arg.Is<LogCriteria>(c =>
                c.MatchId == hybridMatch.MatchId));

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Licensee TransId") &&
                msg.Contains("T1") && msg.Contains("T2") &&
                msg.Contains("Credit TransId") &&
                msg.Contains("T3")), Arg.Any<int>());
    }

    [Fact]
    public async Task InvalidTransId_WhenTransIdsEmpty_ShouldNotWriteLogOrSendMessage()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456
        };
        var transIds = new List<string>();
        var originalTransId = new List<TranInfo>();

        await service.InvalidTransIdsFetchNewOdds(hybridMatch, transIds, originalTransId);

        await mockRepository.DidNotReceive().ExecuteAsync(Arg.Any<LogCriteria>());
        await mockLetsTalkService.DidNotReceive().SendMessage(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Fact]
    public async Task InvalidTransIdsSendLetstalk_WhenWrongOddsTransIds_ShouldSendMessage()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456
        };
        var validationTransIds = new List<ValidationTransIds>
        {
            new() { WrongOddsTransIds = "T1, T2" },
            new() { WrongOddsTransIds = "T3" }
        };
        var originalTransId = new List<TranInfo>
        {
            new() { TransId = "T1", IsCredit = false },
            new() { TransId = "T2", IsCredit = false },
            new() { TransId = "T3", IsCredit = true }
        };

        await service.InvalidTransIdsUpdateOdds(hybridMatch, validationTransIds, originalTransId);

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot settle due to invalid leg odds") &&
                msg.Contains("Licensee TransId") &&
                msg.Contains("T1") && msg.Contains("T2") &&
                msg.Contains("Credit TransId") &&
                msg.Contains("T3")), Arg.Any<int>());
    }

    [Fact]
    public async Task InvalidTransIdsSendLetstalk_WhenWrongOddsTransIdsEmpty_ShouldNotSendMessage()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456
        };
        var validationTransIds = new List<ValidationTransIds>
        {
            new() { WrongOddsTransIds = string.Empty, InvalidTransIds = string.Empty }
        };
        var originalTransId = new List<TranInfo>();

        await service.InvalidTransIdsUpdateOdds(hybridMatch, validationTransIds, originalTransId);

        await mockLetsTalkService.DidNotReceive().SendMessage(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Fact]
    public async Task InvalidTransIdsSendLetstalk_WhenInvalidTransIds_ShouldSendMessage()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456
        };
        var validationTransIds = new List<ValidationTransIds>
        {
            new() { WrongOddsTransIds = string.Empty, InvalidTransIds = "T4, T5" },
            new() { WrongOddsTransIds = string.Empty, InvalidTransIds = "T6" }
        };
        var originalTransId = new List<TranInfo>
        {
            new() { TransId = "T4", IsCredit = false },
            new() { TransId = "T5", IsCredit = false },
            new() { TransId = "T6", IsCredit = true }
        };

        await service.InvalidTransIdsUpdateOdds(hybridMatch, validationTransIds, originalTransId);

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot update new leg odds") &&
                msg.Contains("Licensee TransId") &&
                msg.Contains("T4") && msg.Contains("T5") &&
                msg.Contains("Credit TransId") &&
                msg.Contains("T6")), Arg.Any<int>());
    }

    [Fact]
    public async Task InvalidTransIdsSendLetstalk_WhenBothWrongOddsAndInvalidTransIds_ShouldSendBothMessages()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456
        };
        var validationTransIds = new List<ValidationTransIds>
        {
            new() { WrongOddsTransIds = "T1, T2", InvalidTransIds = "T3, T4" }
        };
        var originalTransId = new List<TranInfo>
        {
            new() { TransId = "T1", IsCredit = false },
            new() { TransId = "T2", IsCredit = true },
            new() { TransId = "T3", IsCredit = false },
            new() { TransId = "T4", IsCredit = true }
        };

        await service.InvalidTransIdsUpdateOdds(hybridMatch, validationTransIds, originalTransId);

        await mockLetsTalkService.Received(2).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Any<string>(),
            Arg.Any<int>());

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot settle due to invalid leg odds") &&
                msg.Contains("Licensee TransId") &&
                msg.Contains("T1") &&
                msg.Contains("Credit TransId") &&
                msg.Contains("T2")),
            Arg.Any<int>());

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot update new leg odds") &&
                msg.Contains("Licensee TransId") &&
                msg.Contains("T3") &&
                msg.Contains("Credit TransId") &&
                msg.Contains("T4")),
            Arg.Any<int>());
    }
}
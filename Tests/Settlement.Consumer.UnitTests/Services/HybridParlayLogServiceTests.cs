using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using NSubstitute;
using Settlement.Consumer.Criteria;
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

        await service.InvalidTransIdsWriteLogAndSendLetstalk(hybridMatch, transIds);

        await mockRepository.Received(1).ExecuteAsync(
            Arg.Is<LogCriteria>(c =>
                c.MatchId == hybridMatch.MatchId &&
                c.Remark == "No Odds Found"));

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot get new Leg odds") &&
                msg.Contains("T1, T2, T3")), Arg.Any<int>());
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

        await service.InvalidTransIdsWriteLogAndSendLetstalk(hybridMatch, transIds);

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

        await service.InvalidTransIdsSendLetstalk(hybridMatch, validationTransIds);

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot settled due to invalid Leg Odds") &&
                msg.Contains("T1") && msg.Contains("T2") && msg.Contains("T3")), Arg.Any<int>());
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

        await service.InvalidTransIdsSendLetstalk(hybridMatch, validationTransIds);

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

        await service.InvalidTransIdsSendLetstalk(hybridMatch, validationTransIds);

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot update new Leg odds") &&
                msg.Contains("T4") && msg.Contains("T5") && msg.Contains("T6")), Arg.Any<int>());
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

        await service.InvalidTransIdsSendLetstalk(hybridMatch, validationTransIds);

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
                msg.Contains("Failed: Cannot settled due to invalid Leg Odds") &&
                msg.Contains("T1") && msg.Contains("T2")),
            Arg.Any<int>());

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot update new Leg odds") &&
                msg.Contains("T3") && msg.Contains("T4")),
            Arg.Any<int>());
    }
}
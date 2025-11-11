using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Services;
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

        await service.InvalidTransId(hybridMatch, transIds);

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

        await service.InvalidTransId(hybridMatch, transIds);

        await mockRepository.DidNotReceive().ExecuteAsync(Arg.Any<LogCriteria>());
        await mockLetsTalkService.DidNotReceive().SendMessage(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Fact]
    public async Task InvalidLegOdds_ShouldSendMessage_WithTransIds()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456
        };
        var transIds = new List<string> { "T1", "T2", "T3" };

        await service.InvalidLegOdds(hybridMatch, transIds);

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot settled due to invalid Leg Odds") &&
                msg.Contains("T1, T2, T3")), Arg.Any<int>());
    }

    [Fact]
    public async Task InvalidLegOdds_WhenTransIdsEmpty_ShouldStillSendMessage()
    {
        var hybridMatch = new HybridMatch
        {
            MatchId = 123,
            UserId = 456
        };
        var transIds = new List<string>();

        await service.InvalidLegOdds(hybridMatch, transIds);

        await mockLetsTalkService.Received(1).SendMessage(
            hybridMatch.MatchId,
            hybridMatch.UserId,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains("Failed: Cannot settled due to invalid Leg Odds")), Arg.Any<int>());
    }
}
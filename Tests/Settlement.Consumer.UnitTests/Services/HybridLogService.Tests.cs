using Fanex.Data.Repository;
using NSubstitute;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Services;
using Settlement.Shared.Services;

namespace Settlement.Consumer.UnitTests.Services;

public class HybridLogServiceTests
{
    private readonly ILetsTalkService mockLetsTalkService;
    private readonly IDynamicRepository mockRepository;
    private readonly HybridLogService service;

    public HybridLogServiceTests()
    {
        mockLetsTalkService = Substitute.For<ILetsTalkService>();
        mockRepository = Substitute.For<IDynamicRepository>();
        service = new HybridLogService(mockLetsTalkService, mockRepository);
    }

    [Fact]
    public async Task WriteLog_ShouldExecuteCriteria_AndReturnActionId()
    {
        var matchId = 123;
        var message = "Test log message";
        var expectedActionId = 456;

        mockRepository.ExecuteAsync(Arg.Any<LogCriteria>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<LogCriteria>();
                criteria.ActionId = expectedActionId;
            });

        var result = await service.WriteLog(matchId);

        Assert.Equal(expectedActionId, result);
        await mockRepository.Received(1).ExecuteAsync(
            Arg.Is<LogCriteria>(c =>
                c.MatchId == matchId &&
                c.Remark == message &&
                c.ActionId == 113));
    }

    [Fact]
    public async Task NotExistLegOdds_ShouldSendMessage_WithLimitedTransIds()
    {
        var matchId = 123;
        var logId = 456;
        var transIds = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7" };

        await service.NotExistLegOdds(matchId, logId, transIds);

        await mockLetsTalkService.Received(1).SendMessage(
            matchId,
            1,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains(logId.ToString()) &&
                msg.Contains("T1, T2, T3, T4, T5") &&
                !msg.Contains("T6") &&
                !msg.Contains("T7")));
    }

    [Fact]
    public async Task NotExistLegOdds_ShouldSendMessage_WithAllTransIds_WhenLessThanFive()
    {
        var matchId = 123;
        var logId = 456;
        var transIds = new[] { "T1", "T2" };

        await service.NotExistLegOdds(matchId, logId, transIds);

        await mockLetsTalkService.Received(1).SendMessage(
            matchId,
            1,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains(logId.ToString()) &&
                msg.Contains("T1, T2")));
    }

    [Fact]
    public async Task InvalidLegOdds_ShouldSendMessage_WithLimitedTransIds()
    {
        var matchId = 123;
        var logId = 456;
        var transIds = new[] { "T1", "T2", "T3", "T4", "T5", "T6" };

        await service.InvalidLegOdds(matchId, logId, transIds);

        await mockLetsTalkService.Received(1).SendMessage(
            matchId,
            1,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains(logId.ToString()) &&
                msg.Contains("invalid Leg Odds") &&
                msg.Contains("T1, T2, T3, T4, T5") &&
                !msg.Contains("T6")));
    }

    [Fact]
    public async Task InvalidLegOdds_ShouldSendMessage_WithCorrectFormat()
    {
        var matchId = 123;
        var logId = 789;
        var transIds = new[] { "T1" };

        await service.InvalidLegOdds(matchId, logId, transIds);

        await mockLetsTalkService.Received(1).SendMessage(
            matchId,
            1,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains($"{logId}  - Failed: Cannot settled due to invalid Leg Odds") &&
                msg.Contains("TransIds: T1")));
    }

    [Fact]
    public async Task NotExistLegOdds_ShouldSendMessage_WithCorrectFormat()
    {
        var matchId = 123;
        var logId = 789;
        var transIds = new[] { "T1" };

        await service.NotExistLegOdds(matchId, logId, transIds);

        await mockLetsTalkService.Received(1).SendMessage(
            matchId,
            1,
            "Get new Leg Odds",
            Arg.Is<string>(msg =>
                msg.Contains($"{logId} - Failed: Cannot get new Leg odds") &&
                msg.Contains("TransIds: T1")));
    }
}
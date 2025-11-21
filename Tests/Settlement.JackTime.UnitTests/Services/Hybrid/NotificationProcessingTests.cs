using Fanex.Data.Repository;
using Microsoft.Extensions.Options;
using NSubstitute;
using Settlement.JackTime.Criteria;
using Settlement.JackTime.Models;
using Settlement.JackTime.Services.Hybrid;
using Settlement.Shared.Models;
using Xunit;

namespace Settlement.JackTime.UnitTests.Services.Hybrid;

public class NotificationProcessingTests
{
    private readonly IGetNotificationService mockGetNotificationService;
    private readonly IDynamicRepository mockRepository;
    private readonly NotificationProcessing service;

    public NotificationProcessingTests()
    {
        mockGetNotificationService = Substitute.For<IGetNotificationService>();
        mockRepository = Substitute.For<IDynamicRepository>();
        var mockOptions = Substitute.For<IOptions<AppSettingsKey>>();

        mockOptions.Value.Returns(new AppSettingsKey
        {
            EnvironmentName = "LOCAL"
        });
        service = new NotificationProcessing(mockGetNotificationService, mockRepository, mockOptions);
    }

    [Fact]
    public async Task ProcessNotifications_ShouldGetOffset_GetNotifications_AndInsertLog()
    {
        var maxOffset = 100L;
        var notification = new Notification(
            (int)maxOffset,
            new List<NotificationDetail>
            {
                new(123, 1, 1.5m),
                new(123, 2, 2.0m)
            }
        );

        mockRepository.ExecuteAsync(Arg.Any<GetOffsetCriteria>())
            .Returns(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = (int)maxOffset;
                return Task.FromResult(0);
            });

        mockGetNotificationService.GetNotifications(maxOffset).Returns(Task.FromResult(notification));

        await service.ProcessNotifications();

        await mockRepository.Received(1).ExecuteAsync(Arg.Any<GetOffsetCriteria>());
        await mockGetNotificationService.Received(1).GetNotifications(maxOffset);
        await mockRepository.Received(1).ExecuteAsync(
            Arg.Is<InsertHybridParlayLogCriteria>(c =>
                c.OffSet == maxOffset &&
                !string.IsNullOrEmpty(c.LegOddsJSON) &&
                c.LegOddsJSON.Contains("123") &&
                c.LegOddsJSON.Contains("1.5") &&
                c.LegOddsJSON.Contains("2.0")));
    }

    [Fact]
    public async Task ProcessNotifications_WhenNotificationIsNull_ShouldNotThrowException()
    {
        var maxOffset = 100L;

        mockRepository.ExecuteAsync(Arg.Any<GetOffsetCriteria>())
            .Returns(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = (int)maxOffset;
                return Task.FromResult(0);
            });

        mockGetNotificationService.GetNotifications(maxOffset).Returns(Task.FromResult<Notification>(null!));

        await service.ProcessNotifications();

        await mockRepository.Received(1).ExecuteAsync(Arg.Any<GetOffsetCriteria>());
        await mockGetNotificationService.Received(1).GetNotifications(maxOffset);
        await mockRepository.DidNotReceive().ExecuteAsync(Arg.Any<InsertHybridParlayLogCriteria>());
    }

    [Fact]
    public async Task ProcessNotifications_WhenNotificationHasEmptyNotifications_ShouldStillInsertLog()
    {
        var maxOffset = 100L;
        var notification = new Notification((int)maxOffset, new List<NotificationDetail>());

        mockRepository.ExecuteAsync(Arg.Any<GetOffsetCriteria>())
            .Returns(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = (int)maxOffset;
                return Task.FromResult(0);
            });

        mockGetNotificationService.GetNotifications(maxOffset).Returns(Task.FromResult(notification));

        await service.ProcessNotifications();

        await mockRepository.Received(1).ExecuteAsync(Arg.Any<GetOffsetCriteria>());
        await mockGetNotificationService.Received(1).GetNotifications(maxOffset);
        await mockRepository.Received(1).ExecuteAsync(
            Arg.Is<InsertHybridParlayLogCriteria>(c =>
                c.OffSet == maxOffset &&
                !string.IsNullOrEmpty(c.LegOddsJSON)));
    }

    [Fact]
    public async Task ProcessNotifications_ShouldSerializeNotificationsToJson()
    {
        var maxOffset = 200L;
        var notification = new Notification(
            (int)maxOffset,
            new List<NotificationDetail>
            {
                new(456, 10, 3.5m),
                new(789, 20, 4.0m)
            }
        );

        mockRepository.ExecuteAsync(Arg.Any<GetOffsetCriteria>())
            .Returns(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = (int)maxOffset;
                return Task.FromResult(0);
            });

        mockGetNotificationService.GetNotifications(maxOffset).Returns(Task.FromResult(notification));

        await service.ProcessNotifications();

        await mockRepository.Received(1).ExecuteAsync(
            Arg.Is<InsertHybridParlayLogCriteria>(c =>
                c.LegOddsJSON.Contains("456") &&
                c.LegOddsJSON.Contains("789") &&
                c.LegOddsJSON.Contains("10") &&
                c.LegOddsJSON.Contains("20") &&
                c.LegOddsJSON.Contains("3.5") &&
                c.LegOddsJSON.Contains("4.0")));
    }

    [Fact]
    public async Task ProcessNotifications_ShouldUseMaxOffsetFromCriteria()
    {
        var maxOffset1 = 50L;
        var maxOffset2 = 150L;
        var notification1 = new Notification((int)maxOffset1, new List<NotificationDetail>());
        var notification2 = new Notification((int)maxOffset2, new List<NotificationDetail>());

        var callCount = 0;
        mockRepository.ExecuteAsync(Arg.Any<GetOffsetCriteria>())
            .Returns(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = callCount == 0 ? (int)maxOffset1 : (int)maxOffset2;
                callCount++;
                return Task.FromResult(0);
            });

        mockGetNotificationService.GetNotifications(Arg.Any<long>()).Returns(Task.FromResult(notification1), Task.FromResult(notification2));

        await service.ProcessNotifications();
        await service.ProcessNotifications();

        await mockGetNotificationService.Received(1).GetNotifications(maxOffset1);
        await mockGetNotificationService.Received(1).GetNotifications(maxOffset2);
    }
}
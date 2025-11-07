using Fanex.Data.Repository;
using FluentAssertions;
using NSubstitute;
using Settlement.JackTime.Criteria;
using Settlement.JackTime.Models;
using Settlement.JackTime.Services.Hybrid;
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
        service = new NotificationProcessing(mockGetNotificationService, mockRepository);
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
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = (int)maxOffset;
            });

        mockGetNotificationService.GetNotifications(maxOffset)
            .Returns(notification);

        mockRepository.ExecuteAsync(Arg.Any<InsertHybridParlayLogCriteria>())
            .Returns(Task.CompletedTask);

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
    public async Task ProcessNotifications_WhenNotificationIsNull_ShouldThrowNullReferenceException()
    {
        var maxOffset = 100L;

        mockRepository.ExecuteAsync(Arg.Any<GetOffsetCriteria>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = (int)maxOffset;
            });

        mockGetNotificationService.GetNotifications(maxOffset)
            .Returns((Notification?)null!);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await service.ProcessNotifications());
        await mockRepository.Received(1).ExecuteAsync(Arg.Any<GetOffsetCriteria>());
        await mockGetNotificationService.Received(1).GetNotifications(maxOffset);
    }

    [Fact]
    public async Task ProcessNotifications_WhenNotificationHasEmptyNotifications_ShouldStillInsertLog()
    {
        var maxOffset = 100L;
        var notification = new Notification((int)maxOffset, new List<NotificationDetail>());

        mockRepository.ExecuteAsync(Arg.Any<GetOffsetCriteria>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = (int)maxOffset;
            });

        mockGetNotificationService.GetNotifications(maxOffset)
            .Returns(notification);

        mockRepository.ExecuteAsync(Arg.Any<InsertHybridParlayLogCriteria>())
            .Returns(Task.CompletedTask);

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
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = (int)maxOffset;
            });

        mockGetNotificationService.GetNotifications(maxOffset)
            .Returns(notification);

        mockRepository.ExecuteAsync(Arg.Any<InsertHybridParlayLogCriteria>())
            .Returns(Task.CompletedTask);

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
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo =>
            {
                var criteria = callInfo.Arg<GetOffsetCriteria>();
                criteria.MaxOffSet = callCount == 0 ? (int)maxOffset1 : (int)maxOffset2;
                callCount++;
            });

        mockGetNotificationService.GetNotifications(Arg.Any<long>())
            .Returns(notification1, notification2);

        mockRepository.ExecuteAsync(Arg.Any<InsertHybridParlayLogCriteria>())
            .Returns(Task.CompletedTask);

        await service.ProcessNotifications();
        await service.ProcessNotifications();

        await mockGetNotificationService.Received(1).GetNotifications(maxOffset1);
        await mockGetNotificationService.Received(1).GetNotifications(maxOffset2);
    }
}


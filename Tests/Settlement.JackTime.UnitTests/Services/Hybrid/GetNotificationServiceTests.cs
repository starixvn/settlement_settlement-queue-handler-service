using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Settlement.JackTime.Models;
using Settlement.JackTime.Services.Hybrid;
using Settlement.Shared.Models;
using Settlement.Shared.Services;
using Xunit;

namespace Settlement.JackTime.UnitTests.Services.Hybrid;

public class GetNotificationServiceTests
{
    private readonly IInvokeExternalAPI mockInvokeExternalApi;
    private readonly IOptions<AppSettingsKey> mockOptions;
    private readonly GetNotificationService service;

    public GetNotificationServiceTests()
    {
        mockInvokeExternalApi = Substitute.For<IInvokeExternalAPI>();
        mockOptions = Substitute.For<IOptions<AppSettingsKey>>();

        mockOptions.Value.Returns(new AppSettingsKey
        {
            InternalApiURL = "https://test-api.com/api/"
        });

        service = new GetNotificationService(mockInvokeExternalApi, mockOptions);
    }

    [Fact]
    public async Task GetNotifications_WhenApiReturnsData_ShouldReturnNotification()
    {
        var offset = 100L;
        var expectedNotification = new Notification(
            (int)offset,
            new List<NotificationDetail>
            {
                new(123, 1, 1.5m),
                new(123, 2, 2.0m)
            }
        );

        var response = new NotificationResponse(expectedNotification, true, null!);
        mockInvokeExternalApi.GetAsync<NotificationResponse>(
            "https://test-api.com/api/jacktime-cash-out/notifications/100")
            .Returns(response);

        var result = await service.GetNotifications(offset);

        result.Should().NotBeNull();
        result.OffSet.Should().Be((int)offset);
        result.Notifications.Should().HaveCount(2);
        result.Notifications.Should().Contain(n => n.MatchId == 123 && n.TransId == 1 && n.Odds == 1.5m);
        result.Notifications.Should().Contain(n => n.MatchId == 123 && n.TransId == 2 && n.Odds == 2.0m);
    }

    [Fact]
    public async Task GetNotifications_WhenApiReturnsNull_ShouldReturnNull()
    {
        var offset = 100L;
        mockInvokeExternalApi.GetAsync<NotificationResponse>(Arg.Any<string>())
            .Returns((NotificationResponse?)null!);

        var result = await service.GetNotifications(offset);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNotifications_WhenApiReturnsNullData_ShouldReturnNull()
    {
        var offset = 100L;
        var response = new NotificationResponse(null, true, null);
        mockInvokeExternalApi.GetAsync<NotificationResponse>(Arg.Any<string>())
            .Returns(response);

        var result = await service.GetNotifications(offset);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNotifications_ShouldCallCorrectUrl()
    {
        var offset = 200L;
        var response = new NotificationResponse(
            new Notification((int)offset, new List<NotificationDetail>()),
            true,
            null);
        mockInvokeExternalApi.GetAsync<NotificationResponse>(Arg.Any<string>())
            .Returns(response);

        await service.GetNotifications(offset);

        await mockInvokeExternalApi.Received(1).GetAsync<NotificationResponse>(
            "https://test-api.com/api/jacktime-cash-out/notifications/200");
    }

    [Fact]
    public async Task GetNotifications_WithDifferentOffset_ShouldUseCorrectOffsetInUrl()
    {
        var offset1 = 50L;
        var offset2 = 150L;
        var response = new NotificationResponse(
            new Notification(0, new List<NotificationDetail>()),
            true,
            null!);
        mockInvokeExternalApi.GetAsync<NotificationResponse>(Arg.Any<string>())
            .Returns(response);

        await service.GetNotifications(offset1);
        await service.GetNotifications(offset2);

        await mockInvokeExternalApi.Received(1).GetAsync<NotificationResponse>(
            "https://test-api.com/api/jacktime-cash-out/notifications/50");
        await mockInvokeExternalApi.Received(1).GetAsync<NotificationResponse>(
            "https://test-api.com/api/jacktime-cash-out/notifications/150");
    }

    [Fact]
    public async Task GetNotifications_WhenNotificationHasEmptyList_ShouldReturnEmptyNotifications()
    {
        var offset = 100L;
        var expectedNotification = new Notification((int)offset, new List<NotificationDetail>());
        var response = new NotificationResponse(expectedNotification, true, null!);
        mockInvokeExternalApi.GetAsync<NotificationResponse>(Arg.Any<string>())
            .Returns(response);

        var result = await service.GetNotifications(offset);

        result.Should().NotBeNull();
        result.OffSet.Should().Be((int)offset);
        result.Notifications.Should().BeEmpty();
    }
}


using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using Microsoft.Extensions.Options;
using NSubstitute;
using Settlement.JackTime.Services;
using Settlement.Shared.Models;
using Settlement.Shared.Services;
using SharpRaven;
using Xunit;

namespace Settlement.JackTime.UnitTests.Services;

public class RequestServiceTests
{
    private readonly IHandleResponseService mockHandleResponseService;
    private readonly IInvokeExternalAPI mockInvokeExternalApi;
    private readonly RequestService requestService;

    public RequestServiceTests()
    {
        var logEngine = new List<ILogEngine>();

        var sentryEngineOptions = new SentryEngineOptions
        {
            Dsn = new Dsn("http://2dfee6c5bbc844558ad3bef5f4b59ee1@sentry.nexdev.net/9")
        };
        logEngine.Add(new SentryLogging(sentryEngineOptions));

        LogManager.SetDefaultLogCategory("NAP-Settlement-LOCAL").Use(logEngine);

        var mockOptions = Substitute.For<IOptions<AppSettingsKey>>();
        mockHandleResponseService = Substitute.For<IHandleResponseService>();
        mockInvokeExternalApi = Substitute.For<IInvokeExternalAPI>();

        mockOptions.Value.Returns(new AppSettingsKey
        {
            ThirdPartyUri = new ThirdPartySettingKeys
            {
                JackTimeTicket = "http://test-api.com"
            }
        });

        requestService = new RequestService(
            mockOptions,
            mockHandleResponseService,
            mockInvokeExternalApi);
    }

    [Fact]
    public async Task UpdateVoidBetTicket_WhenVoidBetInfosIsEmpty_ShouldNotCallExternalApi()
    {
        // Arrange
        var voidBetInfos = new List<VoidBetInfo>();

        // Act
        await requestService.UpdateVoidBetTicket(voidBetInfos, 1);

        // Assert
        await mockInvokeExternalApi.DidNotReceive().PostAsync(Arg.Any<object>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateVoidBetTicket_WhenSingleAction_ShouldCallExternalApiOnce()
    {
        // Arrange
        var voidBetInfos = new List<VoidBetInfo>
        {
            new() { TransId = "T1", CustId = "C1", Action = "VOID" },
            new() { TransId = "T2", CustId = "C2", Action = "VOID" }
        };

        var expectedResponse = new HttpResponseMessage();
        mockInvokeExternalApi.PostAsync(Arg.Any<object>(), Arg.Any<string>())
            .Returns(expectedResponse);

        // Act
        await requestService.UpdateVoidBetTicket(voidBetInfos, 1);

        // Assert
        await mockInvokeExternalApi.Received(1).PostAsync(
            Arg.Is<object>(obj => obj is VoidBetApiModel),
            "http://test-api.com/VoidBet");

        await mockHandleResponseService.Received(1).HandleVoidBetResponse(
            expectedResponse, Arg.Any<VoidBetApiModel>(), 1);
    }

    [Fact]
    public async Task UpdateVoidBetTicket_WhenMultipleActions_ShouldCallExternalApiForEachAction()
    {
        // Arrange
        var voidBetInfos = new List<VoidBetInfo>
        {
            new() { TransId = "T1", CustId = "C1", Action = "VOID" },
            new() { TransId = "T2", CustId = "C2", Action = "UNDO" },
            new() { TransId = "T3", CustId = "C3", Action = "VOID" }
        };

        var expectedResponse = new HttpResponseMessage();
        mockInvokeExternalApi.PostAsync(Arg.Any<object>(), Arg.Any<string>())
            .Returns(expectedResponse);

        // Act
        await requestService.UpdateVoidBetTicket(voidBetInfos, 1);

        // Assert
        await mockInvokeExternalApi.Received(1).PostAsync(
            Arg.Is<object>(obj => obj is VoidBetApiModel),
            "http://test-api.com/VoidBet");

        await mockInvokeExternalApi.Received(1).PostAsync(
            Arg.Is<object>(obj => obj is VoidBetApiModel),
            "http://test-api.com/UndoVoidBet");

        await mockHandleResponseService.Received(2).HandleVoidBetResponse(
            expectedResponse, Arg.Any<VoidBetApiModel>(), 1);
    }

    [Fact]
    public async Task UpdateVoidBetTicket_WhenActionContainsUndo_ShouldUseUndoVoidBetEndpoint()
    {
        // Arrange
        var voidBetInfos = new List<VoidBetInfo>
        {
            new() { TransId = "T1", CustId = "C1", Action = "UNDO_VOID" },
            new() { TransId = "T2", CustId = "C2", Action = "undo_bet" }
        };

        var expectedResponse = new HttpResponseMessage();
        mockInvokeExternalApi.PostAsync(Arg.Any<object>(), Arg.Any<string>())
            .Returns(expectedResponse);

        // Act
        await requestService.UpdateVoidBetTicket(voidBetInfos, 1);

        // Assert
        await mockInvokeExternalApi.Received(2).PostAsync(
            Arg.Is<object>(obj => obj is VoidBetApiModel),
            "http://test-api.com/UndoVoidBet");
    }

    [Fact]
    public async Task UpdateVoidBetTicket_WhenActionDoesNotContainUndo_ShouldUseVoidBetEndpoint()
    {
        // Arrange
        var voidBetInfos = new List<VoidBetInfo>
        {
            new() { TransId = "T1", CustId = "C1", Action = "VOID" },
            new() { TransId = "T2", CustId = "C2", Action = "CANCEL" }
        };

        var expectedResponse = new HttpResponseMessage();
        mockInvokeExternalApi.PostAsync(Arg.Any<object>(), Arg.Any<string>())
            .Returns(expectedResponse);

        // Act
        await requestService.UpdateVoidBetTicket(voidBetInfos, 1);

        // Assert
        await mockInvokeExternalApi.Received(2).PostAsync(
            Arg.Is<object>(obj => obj is VoidBetApiModel),
            "http://test-api.com/VoidBet");
    }

    [Fact]
    public async Task UpdateVoidBetTicket_ShouldGroupVoidBetInfosByAction()
    {
        // Arrange
        var voidBetInfos = new List<VoidBetInfo>
        {
            new() { TransId = "T1", CustId = "C1", Action = "VOID" },
            new() { TransId = "T2", CustId = "C2", Action = "VOID" },
            new() { TransId = "T3", CustId = "C3", Action = "UNDO" }
        };

        var expectedResponse = new HttpResponseMessage();
        mockInvokeExternalApi.PostAsync(Arg.Any<object>(), Arg.Any<string>())
            .Returns(expectedResponse);

        // Act
        await requestService.UpdateVoidBetTicket(voidBetInfos, 1);

        // Assert
        await mockInvokeExternalApi.Received().PostAsync(
            Arg.Is<object>(obj => obj is VoidBetApiModel),
            Arg.Any<string>());

        await mockInvokeExternalApi.Received().PostAsync(
            Arg.Is<object>(obj => obj is VoidBetApiModel),
            Arg.Any<string>());
    }
}
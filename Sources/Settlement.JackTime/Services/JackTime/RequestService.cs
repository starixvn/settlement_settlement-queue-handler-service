using Fanex.Settlement.MessageBridge.JackTime.Constants;
using Microsoft.Extensions.Options;
using Settlement.Shared.Models;
using Settlement.Shared.Services;

namespace Settlement.JackTime.Services.JackTime;

public interface IRequestService
{
    Task UpdateVoidBetTicket(IList<VoidBetInfo> voidBetInfos, int matchId);
}

public class RequestService(
    IOptions<AppSettingsKey> options,
    IHandleResponseService handleResponseService,
    IInvokeExternalAPI invokeExternalApi) : IRequestService
{
    public async Task UpdateVoidBetTicket(IList<VoidBetInfo> voidBetInfos, int matchId)
    {
        var actions = voidBetInfos.GroupBy(m => m.Action).Select(m => m.Key);
        foreach (var action in actions)
        {
            var voidBetByAction = voidBetInfos.Where(m => m.Action == action).ToList();
            var actionApi = action.Contains("UNDO", StringComparison.OrdinalIgnoreCase) ? CashOutAction.UndoVoidBet : CashOutAction.VoidBet;
            var apiUri = $"{options.Value.ThirdPartyUri.JackTimeTicket}/{actionApi}";
            var voidBetApiModel = new VoidBetApiModel
            {
                VoidBetInfos = voidBetByAction
            };

            var response = await invokeExternalApi.PostAsync(voidBetApiModel, apiUri);
            await handleResponseService.HandleVoidBetResponse(response, voidBetApiModel, matchId);
        }
    }
}
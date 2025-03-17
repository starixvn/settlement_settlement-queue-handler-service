using Fanex.Settlement.MessageBridge.JackTime;
using Fanex.Settlement.MessageBridge.JackTime.Constants;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Settlement.Consumer.Models;
using Settlement.Consumer.Models.SoccerJackTime;

namespace Settlement.Consumer.Services;
public interface IRequestService
{
    JTResultMarketRequestModel CreateSoccerJTRequest(IEnumerable<Market> markets, IEnumerable<JackTimeResult> result);

    Task<List<Market>> GetMarkets(int matchId);

    Task UpdateMarkets(JTResultMarketRequestModel request, IEnumerable<Market> markets, int matchId);

    Task UpdateVoidBetTicket(VoidBetRequest request, int matchId);
}


public class RequestService(
    IInvokeExternalAPI invokeExternalApi,
    IOptions<AppSettingsKey> options,
    IHandleResponseService handleResponseService) : IRequestService
{

    public async Task<List<Market>> GetMarkets(int matchId)
    {
        var result =
            await invokeExternalApi.GetAsync<SoccerMarketItem>(
                $"{options.Value.ThirdPartyUri.JackTimeMarkets}?MatchIds={matchId}");

        return result is { IsSuccess: true } ? result.Data.ToList() : [];
    }

    public async Task UpdateMarkets(JTResultMarketRequestModel request, IEnumerable<Market> markets, int matchId)
    {

        var response = await invokeExternalApi.PutAsync(request, options.Value.ThirdPartyUri.JackTimeResultMarkets);

        await handleResponseService.HandleUpdateResultResponse(response, markets, matchId);
    }


    public JTResultMarketRequestModel CreateSoccerJTRequest(IEnumerable<Market> markets, IEnumerable<JackTimeResult> result)
    {
        return new JTResultMarketRequestModel
        {
            Header = new HeaderRequest
            {
                Transaction_Id = new TransactionId
                {
                    Id = Guid.NewGuid().ToString()
                }
            },
            Request = new MarketModel
            {
                Markets = markets.Select(m => new MarketData
                {
                    Market = m,
                    WayScores = GenerateOutCome(result, m.BetType, m.GoalLine)
                }).Where(m => m.WayScores.Any())
            }
        };
    }


    private static IEnumerable<WayScore> GenerateOutCome(IEnumerable<JackTimeResult> result, int betType, decimal goalLine)
    {
        var groupBetType = result.Where(r => r.Bettype == betType && goalLine == r.Hdp).ToList();

        if (groupBetType.IsNullOrEmpty())
        {
            return [];
        }

        return groupBetType.Select(r => new WayScore
        {
            Id = r.Way,
            Outcome = r.OutCome
        });
    }

    public async Task UpdateVoidBetTicket(VoidBetRequest request, int matchId)
    {
        var actions = request.VoidBetInfos.GroupBy(m => m.Action).Select(m => m.Key);
        foreach (var action in actions)
        {
            var voidBetInfos = request.VoidBetInfos.Where(m => m.Action == action);
            var actionApi = action.Contains("UNDO", StringComparison.OrdinalIgnoreCase) ? CashOutAction.UndoVoidBet : CashOutAction.VoidBet;
            var apiUri = $"{options.Value.ThirdPartyUri.JackTimeTicket}/{actionApi}";
            var voidBetApiModel = new VoidBetApiModel
            {
                VoidBetInfos = voidBetInfos
            };

            var response = await invokeExternalApi.PostAsync(voidBetApiModel, apiUri);
            await handleResponseService.HandleVoidBetResponse(response, voidBetApiModel, matchId);
        }
    }
}


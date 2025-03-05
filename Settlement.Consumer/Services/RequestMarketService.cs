using Microsoft.Extensions.Options;
using Settlement.Consumer.Models;
using Settlement.Consumer.Models.SoccerJackTime;

namespace Settlement.Consumer.Services;
public interface IRequestMarketService
{
    JTResultMarketRequestModel CreateSoccerJTRequest(IEnumerable<Market> markets, IEnumerable<JackTimeResult> result);

    Task<List<Market>> GetMarkets(int matchId);

    Task UpdateMarkets(JTResultMarketRequestModel request);
}


public class RequestMarketService(IInvokeExternalAPI invokeExternalApi, IOptions<AppSettingsKey> options) : IRequestMarketService
{

    public async Task<List<Market>> GetMarkets(int matchId)
    {
        var result =
            await invokeExternalApi.GetAsync<SoccerMarketItem>(
                $"{options.Value.ThirdPartyUri.JackTimeMarkets}?MatchIds={matchId}");

        return result is { IsSuccess: true } ? result.Data.ToList() : [];
    }

    public async Task UpdateMarkets(JTResultMarketRequestModel request)
    {

        var response = await invokeExternalApi.PutAsync(request, options.Value.ThirdPartyUri.JackTimeResultMarkets);

        if (!response.IsSuccess)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Soccer JackTime Update Market Failed: {response.Message}");
        }
    }


    public JTResultMarketRequestModel CreateSoccerJTRequest(IEnumerable<Market> markets, IEnumerable<JackTimeResult> result)
    {
        return new JTResultMarketRequestModel
        {
            Header = new HeaderRequest
            {
                Transaction_Id = new TransactionId()
                {
                    Id = Guid.NewGuid().ToString()
                }
            },
            Request = new MarketModel()
            {
                Markets = markets.Select(m => new MarketData
                {
                    Market = m,
                    WayScores = GenerateOutCome(result, m.BetType)
                })
            }
        };
    }


    private static IEnumerable<WayScore> GenerateOutCome(IEnumerable<JackTimeResult> result, int betType)
    {
        var groupBetType = result.Where(r => r.Bettype == betType);

        return groupBetType.Select(r => new WayScore
        {
            Id = r.Bettype,
            Outcome = r.OutCome
        });
    }
}


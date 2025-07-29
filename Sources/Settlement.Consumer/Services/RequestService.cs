using Fanex.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Settlement.Consumer.Constants;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Shared.Models;
using Settlement.Shared.Services;

namespace Settlement.Consumer.Services;

public interface IRequestService
{
    JTResultMarketRequestModel CreateSoccerJTRequest(IEnumerable<Market> markets, IEnumerable<JackTimeResult> result);

    Task<List<Market>> GetMarkets(int matchId);

    Task UpdateMarkets(JTResultMarketRequestModel request, IEnumerable<Market> markets, int matchId);
}

public class RequestService(
    IInvokeExternalAPI invokeExternalApi,
    IOptions<AppSettingsKey> options,
    IHandleResponseService handleResponseService) : IRequestService
{
    public async Task<List<Market>> GetMarkets(int matchId)
    {
        var marketApi = $"{options.Value.ThirdPartyUri.JackTimeMarkets}/{CashOutEndpoints.GetMarkets}";
        var result = await invokeExternalApi.GetAsync<SoccerMarketItem>($"{marketApi}/?MatchIds={matchId}");
        await LogService.InfoAsync($"Get Markets API: {JsonConvert.SerializeObject(result)}");

        return result is { IsSuccess: true } ? result.Data.ToList() : [];
    }

    public async Task UpdateMarkets(JTResultMarketRequestModel request, IEnumerable<Market> markets, int matchId)
    {
        await LogService.InfoAsync($"Request Update Result Match {matchId}: " + JsonConvert.SerializeObject(request));
        var updateMarketApi = $"{options.Value.ThirdPartyUri.JackTimeMarkets}/{CashOutEndpoints.ResultMarkets}";
        var response = await invokeExternalApi.PutAsync(request, updateMarketApi);

        await handleResponseService.HandleUpdateResultResponse(response, markets, matchId);
    }

    public JTResultMarketRequestModel CreateSoccerJTRequest(IEnumerable<Market> markets, IEnumerable<JackTimeResult> result)
    {
        return new JTResultMarketRequestModel
        {
            Markets = markets.Select(m => new MarketData
            {
                Market = m,
                WayScores = GenerateOutCome(result, m.BetType, m.GoalLine)
            }).Where(m => m.WayScores.Any())
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
}
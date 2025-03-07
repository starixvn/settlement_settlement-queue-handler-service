using AutoMapper;
using Fanex.Data.Repository;
using Fanex.RemotingRef;
using Fanex.Settlement.MessageBridge.JackTime;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models;
using Settlement.Consumer.Models.SoccerJackTime;

namespace Settlement.Consumer.Services;
public interface IHandleResponseService
{
    public Task HandleVoidBetResponse(HttpResponseMessage response, VoidBetApiModel model, int matchId);

    Task HandleUpdateResultResponse(HttpResponseMessage response, IEnumerable<Market> markets, int matchId);
}

public class HandleResponseService(SendMailService sendMailService, IMapper mapper, IRemotingRefService remotingRefService, IDynamicRepository repository) : IHandleResponseService
{


    public async Task HandleVoidBetResponse(HttpResponseMessage response, VoidBetApiModel model, int matchId)
    {
        var matchInfo = await GetMatchInfo(matchId);
        if (!response.IsSuccessStatusCode)
        {
            var mailModel = mapper.Map<MatchDetail, NotificationModel>(matchInfo);
            mailModel.RefDataService = remotingRefService;
            mailModel.Message = await response.Content.ReadAsStringAsync();
            mailModel.Action = $"CO-{mailModel.Action}-JackTime";
            mailModel.StartTime = DateTimeOffset.Now.DateTime;
            mailModel.VoidBetInfos = model.VoidBetInfos;
            sendMailService.SendMail(mailModel, true);
        }
    }

    public async Task HandleUpdateResultResponse(HttpResponseMessage response, IEnumerable<Market> markets, int matchId)
    {
        var matchInfo = await GetMatchInfo(matchId);
        if (!response.IsSuccessStatusCode)
        {
            var mailModel = mapper.Map<MatchDetail, NotificationModel>(matchInfo);
            mailModel.RefDataService = remotingRefService;
            mailModel.Message = await response.Content.ReadAsStringAsync();
            mailModel.Action = $"CO-{mailModel.Action}-JackTime";
            mailModel.StartTime = DateTimeOffset.Now.DateTime;
            mailModel.Markets = markets;
            sendMailService.SendMail(mailModel, false);
        }
    }

    private async Task<MatchDetail> GetMatchInfo(int matchId)
    {
        var criteria = new GetMatchDetailCriteria
        {
            MatchId = matchId
        };

        return await repository.GetAsync<MatchDetail>(criteria);
    }
}

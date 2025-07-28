using AutoMapper;
using Fanex.Data.Repository;
using Fanex.RemotingRef;
using Settlement.Shared.Criteria;
using Settlement.Shared.Models;

namespace Settlement.Shared.Services;

public interface IHandleResponseService
{
    public Task HandleVoidBetResponse(HttpResponseMessage response, VoidBetApiModel model, int matchId);

    Task HandleUpdateResultResponse(HttpResponseMessage response, IEnumerable<Market> markets, int matchId);
}

public class HandleResponseService(SendMailService sendMailService, IMapper mapper, IRemotingRefService remotingRefService, IDynamicRepository repository) : IHandleResponseService
{
    public async Task HandleVoidBetResponse(HttpResponseMessage response, VoidBetApiModel model, int matchId)
    {
        await HandleResponse(response, matchId, mailModel =>
       {
           mailModel.VoidBetInfos = model.VoidBetInfos;
           sendMailService.SendMail(mailModel, true);
       });
    }

    public async Task HandleUpdateResultResponse(HttpResponseMessage response, IEnumerable<Market> markets, int matchId)
    {
        await HandleResponse(response, matchId, mailModel =>
        {
            mailModel.Markets = markets;
            sendMailService.SendMail(mailModel, false);
        });
    }

    private async Task HandleResponse(HttpResponseMessage response, int matchId, Action<NotificationModel> configureMailModel)
    {
        var matchInfo = await GetMatchInfo(matchId);
        if (!response.IsSuccessStatusCode)
        {
            var mailModel = mapper.Map<MatchDetail, NotificationModel>(matchInfo);
            mailModel.RefDataService = remotingRefService;
            mailModel.Message = await response.Content.ReadAsStringAsync();
            mailModel.Action = $"CO-{mailModel.Action}-JackTime";
            mailModel.StartTime = DateTimeOffset.Now.DateTime;
            configureMailModel(mailModel);
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
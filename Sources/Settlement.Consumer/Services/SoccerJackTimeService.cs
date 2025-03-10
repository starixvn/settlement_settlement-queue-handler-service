using AutoMapper;
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models;
using Settlement.Consumer.Models.SoccerJackTime;
using System.Xml.Linq;

namespace Settlement.Consumer.Services;
public interface ISoccerJackTimeService
{
    Task UpdateResultMarket(SoccerResult result, IList<Market> marketOffered);

    Task UpdateTicket(SoccerResult result);
}

public class SoccerJackTimeService(
    IDynamicRepository repository,
    IMapper mapper,
    IRequestService requestService,
    IOptions<AppSettingsKey> options) : ISoccerJackTimeService
{
    public async Task UpdateResultMarket(SoccerResult result, IList<Market> marketOffered)
    {
        var criteria = mapper.Map<SoccerResult, CashOutSelCriteria>(result);
        criteria.Markets = JsonConvert.SerializeObject(marketOffered);

        var results = await repository.FetchAsync<JackTimeResult>(criteria);

        var soccerJackTimeRequest = requestService.CreateSoccerJTRequest(marketOffered, results);

        await requestService.UpdateMarkets(soccerJackTimeRequest, marketOffered, result.MatchId);
    }

    public async Task UpdateTicket(SoccerResult result)
    {
        if (result.Action.Contains("CAL"))
        {
            return;
        }
        var criteria = mapper.Map<SoccerResult, SoccerGetTicketInfoCriteria>(result);
        var getTicketInfoTasks = options.Value.SiteDbServer.Select(s => GetTicketInfo(s, criteria));
        var responseTickets = await Task.WhenAll(getTicketInfoTasks.ToArray());
        var ticketInfos = responseTickets.SelectMany(t => t).ToList();
        var validTicketInfos = ticketInfos.Where(filterValidTicketInfos).ToList();

        if (validTicketInfos.Any())
        {
            await requestService.UpdateVoidBetTicket(new VoidBetRequest
            {
                VoidBetInfos = validTicketInfos,
                MatchId = result.MatchId,
                Action = result.Action
            }, result.MatchId);
        }
    }

    private bool filterValidTicketInfos(VoidBetInfo voidBetInfo)
    {
        var element = XElement.Parse(voidBetInfo.ExtraInfo);

        var jtseqidAttr = element.Attribute("jtseqid");

        if (jtseqidAttr == null)
        {
            return false;
        }
        var jtseqidValue = jtseqidAttr.Value.Trim();

        return !string.IsNullOrEmpty(jtseqidValue);
    }

    private async Task<IEnumerable<VoidBetInfo>> GetTicketInfo(string server, SoccerGetTicketInfoCriteria criteria)
    {
        criteria.DbServer = server;

        return await repository.FetchAsync<VoidBetInfo>(criteria);
    }
}

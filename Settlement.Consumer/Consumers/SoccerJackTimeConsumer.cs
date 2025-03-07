using AutoMapper;
using Fanex.Data.Repository;
using Fanex.Settlement.MessageBridge.JackTime;
using MassTransit;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Settlement.Consumer.Criteria;
using Settlement.Consumer.Models;
using Settlement.Consumer.Models.SoccerJackTime;
using Settlement.Consumer.Services;

namespace Settlement.Consumer.Consumers;
public class SoccerJackTimeConsumer(
    IRequestService requestService,
    IDynamicRepository repository,
    IMapper mapper,
    IOptions<AppSettingsKey> options) : IConsumer<SoccerResult>
{
    public async Task Consume(ConsumeContext<SoccerResult> context)
    {
        try
        {
            var marketOffered = await requestService.GetMarkets(context.Message.MatchId);
            if (!marketOffered.Any())
            {
                return;
            }

            await UpdateTicket(context.Message);
            await UpdateResultMarket(context.Message, marketOffered);
        }
        catch (Exception e)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Soccer JackTime Update Market Failed: {e.Message}");
        }
    }

    private async Task UpdateResultMarket(SoccerResult result, IList<Market> marketOffered)
    {
        var criteria = mapper.Map<SoccerResult, CashOutSelCriteria>(result);
        criteria.Markets = JsonConvert.SerializeObject(marketOffered);

        var results = await repository.FetchAsync<JackTimeResult>(criteria);

        var soccerJackTimeRequest = requestService.CreateSoccerJTRequest(marketOffered, results);

        await requestService.UpdateMarkets(soccerJackTimeRequest, marketOffered, result.MatchId);
    }

    private async Task UpdateTicket(SoccerResult result)
    {
        if (result.Action.Contains("CAL"))
        {
            return;
        }
        var criteria = mapper.Map<SoccerResult, SoccerGetTicketInfoCriteria>(result);
        var getTicketInfoTasks = options.Value.SiteDbServer.Select(s => GetTicketInfo(s, criteria));
        var responseTickets = await Task.WhenAll(getTicketInfoTasks.ToArray());
        var ticketInfos = responseTickets.SelectMany(t => t).ToList();
        if (ticketInfos.Any())
        {
            await requestService.UpdateVoidBetTicket(new VoidBetRequest
            {
                VoidBetInfos = ticketInfos,
                MatchId = result.MatchId,
                Action = result.Action
            }, result.MatchId);
        }
    }

    private async Task<IEnumerable<VoidBetInfo>> GetTicketInfo(string server, SoccerGetTicketInfoCriteria criteria)
    {
        criteria.DbServer = server;

        return await repository.FetchAsync<VoidBetInfo>(criteria);
    }
}

using Settlement.JackTime.Models;
using Settlement.Shared.Models;

namespace Settlement.JackTime.Services;

public interface ISoccerJackTimeService
{
    Task UpdateTicket(Match match);
}

public class SoccerJackTimeService(
    IRequestService requestService) : ISoccerJackTimeService
{
    public async Task UpdateTicket(Match match)
    {
        var validTicketInfos = match.VoidBetInfos.Where(FilterValidTicketInfos).ToList();
        if (validTicketInfos.Count == 0)
        {
            return;
        }

        await requestService.UpdateVoidBetTicket(validTicketInfos, match.MatchId);
    }

    private static bool FilterValidTicketInfos(VoidBetInfo voidBetInfo)
    {
        return !string.IsNullOrEmpty(voidBetInfo.ExtraInfo) &&
               voidBetInfo.ExtraInfo.Contains("jtseqid", StringComparison.OrdinalIgnoreCase);
    }
}
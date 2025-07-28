using Settlement.JackTime.Models;
using Settlement.Shared.Models;
using System.Xml.Linq;

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
        var xmlDoc = XDocument.Parse($"<root>{voidBetInfo.ExtraInfo}</root>");

        var jtseqidAttr = xmlDoc.Descendants()
            .Select(e => e.Attribute("jtseqid"))
            .FirstOrDefault(attr => attr != null);

        return jtseqidAttr != null && !string.IsNullOrEmpty(jtseqidAttr.Value);
    }
}
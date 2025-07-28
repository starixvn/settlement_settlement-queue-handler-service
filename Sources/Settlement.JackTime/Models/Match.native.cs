using Settlement.Shared.Models;

namespace Settlement.JackTime.Models;
public class Match
{
    public int MatchId { get; set; }

    public int LeagueId { get; set; }

    public IList<VoidBetInfo> VoidBetInfos { get; set; } = new List<VoidBetInfo>();
}

namespace Settlement.Shared.Models;
public class Market
{
    public int MatchId { get; set; }
    public int BetType { get; set; }
    public decimal GoalLine { get; set; }

    public int LiveHomeScore { get; set; }

    public int LiveAwayScore { get; set; }
}

using Settlement.Shared.Models;

namespace Settlement.Consumer.Models.SoccerJackTime;

public class JTResultMarketRequestModel
{
    public IEnumerable<MarketData> Markets { get; set; } = [];
}

public class MarketData
{
    public Market Market { get; set; }
    public IEnumerable<WayScore> WayScores { get; set; } = [];
}

public class WayScore
{
    public int Id { get; set; }
    public int Outcome { get; set; }
}


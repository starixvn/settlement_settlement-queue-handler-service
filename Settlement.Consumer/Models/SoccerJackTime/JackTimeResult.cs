using Fanex.Data;

namespace Settlement.Consumer.Models.SoccerJackTime;
public class JackTimeResult
{
    public int Bettype { get; set; }

    [SpParam(SqlParamName = "hdp")]
    public decimal Handicap { get; set; }

    public string BetTeam { get; set; }

    public int OutCome { get; set; }

    public string ResourceId { get; set; }
}

using System.Text.Json.Serialization;

namespace Settlement.Shared.Models;
public class VoidBetInfo
{
    [JsonIgnore]
    public int MatchId { get; set; }

    [JsonIgnore]
    public int LeagueId { get; set; }

    public string TransId { get; set; }

    public string CustId { get; set; }

    public string Action { get; set; }

    [JsonIgnore]
    public string ExtraInfo { get; set; }
}

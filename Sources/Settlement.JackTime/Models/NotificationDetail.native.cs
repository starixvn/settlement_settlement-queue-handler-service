using Newtonsoft.Json;

namespace Settlement.JackTime.Models;

public record NotificationDetail(int MatchId, [JsonProperty("transId")] long BetId, decimal Odds);
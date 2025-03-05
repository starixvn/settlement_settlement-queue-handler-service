using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Settlement.Consumer.Models.SoccerJackTime;
public class MarketId
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonProperty("Id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonProperty("Id")]
    public List<string> MultipleIds { get; set; }

}

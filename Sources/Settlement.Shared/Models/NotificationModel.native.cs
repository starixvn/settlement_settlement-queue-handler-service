using Fanex.RemotingRef;
using System.Text.Json.Serialization;

namespace Settlement.Shared.Models;

public class NotificationModel
{
    private const string LanguageCode = "en";

    private IEnumerable<VoidBetInfo> voidBetInfos;

    private IEnumerable<Market> markets;

    [JsonIgnore]
    public IRemotingRefService RefDataService { get; set; }

    public int HomeId { get; set; }

    public string HomeName => RefDataService.GetTeamName(HomeId.ToString(), LanguageCode, string.Empty);

    public int AwayId { get; set; }

    public string AwayName => RefDataService.GetTeamName(AwayId.ToString(), LanguageCode, string.Empty);

    public string MatchCode { get; set; }

    public int MatchId { get; set; }

    public string Action { get; set; }

    public string Message { get; set; }

    public DateTime StartTime { get; set; }

    public string StartDate
    {
        get
        {
            var date = StartTime.ToString("MMMM dd, yyyy");
            var time = StartTime.ToString("h:mm ttt");
            return $"{date} at {time}";
        }
    }

    public int SportType { get; set; }

    public IEnumerable<VoidBetInfo> VoidBetInfos
    {
        get => voidBetInfos ?? [];

        set => voidBetInfos = value;
    }

    public IEnumerable<Market> Markets
    {
        get => markets ?? [];

        set => markets = value;
    }
}
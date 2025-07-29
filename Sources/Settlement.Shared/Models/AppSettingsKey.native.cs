namespace Settlement.Shared.Models;

public class AppSettingsKey
{
    public ThirdPartySettingKeys ThirdPartyUri { get; set; }

    public string EnvironmentName { get; set; }
}

public class ThirdPartySettingKeys
{
    public string JackTimeTicket { get; set; }

    public string JackTimeMarkets { get; set; }

    public string JackTimeResultMarkets { get; set; }
}
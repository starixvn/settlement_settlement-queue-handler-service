namespace Settlement.Consumer.Models;
public class AppSettingsKey
{
    public required ThirdPartySettingKeys ThirdPartyUri { get; set; }

    public List<string> SiteDbServer { get; set; }
}


public class ThirdPartySettingKeys
{
    public string JackTimeMarkets { get; set; }

    public string JackTimeResultMarkets { get; set; }
}
namespace Settlement.Consumer.Models;
public class AppSettingsKey
{
    public ThirdPartySettingKeys ThirdPartyUri { get; set; }

    public List<string> SiteDbServer { get; set; }
}


public class ThirdPartySettingKeys
{

    public string JackTimeTicket { get; set; }
    public string JackTimeMarkets { get; set; }

    public string JackTimeResultMarkets { get; set; }
}
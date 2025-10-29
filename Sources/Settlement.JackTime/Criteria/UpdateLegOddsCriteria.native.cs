public class UpdateLegOddsCriteria : INonQueryCommand
{
    public string LegOddsJSON { get; set; }

    public string GetSettingKey()
    {
        return "NAP_Admin_HybridParlay_LegOddsUpd";
    }

    public bool IsValid()
    {
        return LegOddsJSON.Length > 0;
    }
}
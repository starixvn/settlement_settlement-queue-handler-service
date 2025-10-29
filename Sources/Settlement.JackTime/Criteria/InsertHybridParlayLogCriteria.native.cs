public class InsertHybridParlayLogCriteria : INonQueryCommand
{
    public string LegOddsJSON { get; set; }

    public long OffSet { get; set; }

    public string ErrMsg { get; set; }

    public int ErrCode { get; set; }
    public string GetSettingKey()
    {
        return "NAP_Admin_HybridParlayLog_Ins";
    }

    public bool IsValid()
    {
        return LegOddsJSON.Length > 0;
    }
}
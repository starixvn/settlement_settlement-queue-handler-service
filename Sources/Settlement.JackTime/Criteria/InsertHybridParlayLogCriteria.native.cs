namespace Settlement.JackTime.Criteria;

public class InsertHybridParlayLogCriteria : INonQueryCommand
{
    public string LegOddsJSON { get; set; }

    public long OffSet { get; set; }

    public string Environment { get; set; }

    public string ErrMsg { get; set; }

    public int ErrCode { get; set; }

    public string GetSettingKey() => "NAP_Admin_HybridParlayLog_Ins";

    public bool IsValid() => !string.IsNullOrEmpty(LegOddsJSON);
}

using Fanex.Data;

namespace Settlement.JackTime.Criteria;

public class UpdateLegOddsCriteria : INonQueryCommand
{
    public string LegOddsJSON { get; set; }

    public int MatchId { get; set; }

    public int Creator { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public string GetSettingKey()
    {
        return $"NAP_Admin_HybridParlay_LegOddsUpd_{DbServer}";
    }

    public bool IsValid()
    {
        return LegOddsJSON.Length > 0;
    }
}
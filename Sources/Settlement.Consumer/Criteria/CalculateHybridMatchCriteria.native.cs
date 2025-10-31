using Fanex.Data;
using Fanex.Data.Repository;

namespace Settlement.Consumer.Criteria;

public class CalculateHybridMatchCriteria : INonQueryCommand
{
    public int MatchId { get; set; }

    public int Creator { get; set; }

    public string Action { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public string GetSettingKey()
    {
        return $"NAP_Admin_Calculate_HybridTransID_{DbServer}";
    }

    public bool IsValid()
    {
        return MatchId > 0;
    }
}
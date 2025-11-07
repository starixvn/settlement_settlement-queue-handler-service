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

    public string GetSettingKey() => $"NAP_Admin_Calculate_HybridParlay_{DbServer}";

    public bool IsValid() => MatchId > 0;
}
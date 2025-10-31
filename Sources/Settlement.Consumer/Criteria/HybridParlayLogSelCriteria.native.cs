using Fanex.Data.Repository;

namespace Settlement.Consumer.Criteria;

public class HybridParlayLogSelCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public DateTimeOffset LogDate { get; set; }

    public override string GetSettingKey() => "NAP_Admin_HybridParlayLog_Sel";

    public override bool IsValid() => MatchId > 0;
}
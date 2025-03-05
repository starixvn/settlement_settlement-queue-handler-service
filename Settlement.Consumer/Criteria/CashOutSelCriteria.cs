using Fanex.Data;
using Fanex.Data.Repository;

namespace Settlement.Consumer.Criteria;
public class CashOutSelCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public string SelectedResources { get; set; }

    public bool DeterminedType { get; set; }

    public string Action { get; set; }

    public string Markets { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }


    public override string GetSettingKey() => $"NAP_Settlement_1_CashoutSel_{DbServer}";

    public override bool IsValid() => MatchId > 0;
}

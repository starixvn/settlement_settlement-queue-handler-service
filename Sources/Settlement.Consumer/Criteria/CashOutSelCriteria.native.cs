using Fanex.Data.Repository;

namespace Settlement.Consumer.Criteria;
public class CashOutSelCriteria : CriteriaBase
{
    public string SelectedResources { get; set; }

    public string Action { get; set; }

    public string Markets { get; set; }

    public override string GetSettingKey() => $"NAP_Settlement_1_CashoutSel";

    public override bool IsValid() => Markets.Length > 0 && SelectedResources.Length > 0;
}

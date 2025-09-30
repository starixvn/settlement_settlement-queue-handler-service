using Fanex.Data;

namespace Settlement.JackTime.Criteria;
public class CashOutTicketSelCriteria : CriteriaBase
{
    public bool IsTest { get; set; }
    
    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey() => $"NAP_Settlement_1_CashoutTicketSel_{DbServer}";

    public override bool IsValid() => true;
}

using Fanex.Data;
using Fanex.Data.Repository;

namespace Settlement.Consumer.Criteria;

public class SoccerGetTicketInfoCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public string SelectedResources { get; set; }

    public string Action { get; set; }

    public byte SDTRecord { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey() => $"NAP_Settlement_1_CashoutTicketSel_{DbServer}";

    public override bool IsValid() => MatchId > 0;
}

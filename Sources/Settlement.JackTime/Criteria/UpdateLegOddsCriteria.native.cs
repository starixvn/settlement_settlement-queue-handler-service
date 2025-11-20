namespace Settlement.JackTime.Criteria;

public class UpdateLegOddsCriteria : CriteriaBase
{
    public string LegOddsJSON { get; set; }

    public int MatchId { get; set; }

    public int Creator { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_HybridParlay_LegOddsUpd_{DbServer}";

    public override bool IsValid() => !string.IsNullOrEmpty(LegOddsJSON);
}
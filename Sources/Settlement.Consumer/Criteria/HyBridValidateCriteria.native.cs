using Fanex.Data;

namespace Settlement.Consumer.Criteria;

public class HyBridValidateCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey() => $"NAP_Set_HybridParlay_Validate_{DbServer}";

    public override bool IsValid() => MatchId > 0;
}
using Fanex.Data.Repository;

namespace Settlement.Consumer.Criteria;
internal class GetMatchDetailCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_MatchSelInfo";

    public override bool IsValid() => MatchId > 0;
}

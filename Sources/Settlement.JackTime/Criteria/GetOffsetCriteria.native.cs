public class GetOffsetCriteria : NonQueryCommand
{
    public int MaxOffSet { get; set; }

    public override string GetSettingKey() => "NAP_Admin_HybridParlayLogOffset_Sel";

    public override bool IsValid() => true;
}
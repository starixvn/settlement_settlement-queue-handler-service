using Fanex.Data.Repository;

public class LetsTalkMessageInsertCriteria : NonQueryCommand
{
    public int MessageGroupId => 2;

    public string MessageType => "Reminder";

    public int MatchId { get; set; }

    public string ActionName { get; set; }

    public string MessageContent { get; set; }

    public int UserId { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public override bool IsValid() => MatchId > 0;

    public override string GetSettingKey() => "NAP_Set_LetstalkMessage_Insert";
}
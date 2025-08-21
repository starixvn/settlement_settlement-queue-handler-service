using Fanex.Data.Repository;

namespace Settlement.Consumer.Criteria;
public class LogCriteria : NonQueryCommand
{

    public LogCriteria(int matchId, string errorMessage, int actionId)
    {
        Remark = errorMessage;
        ActionId = actionId;
        MatchId = matchId;
    }

    public LogCriteria()
    {
    }

    public int UserId { get; set; } = 0;

    public int? MatchId { get; set; }

    public string Remark { get; set; }
    public string RemarkJson { get; set; }

    public string Ip => string.Empty;

    public int ActionId { get; set; }

    public override bool IsValid() => true;

    public override string GetSettingKey() => "WriteLog_UserLog_Settle";
}
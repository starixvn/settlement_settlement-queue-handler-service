using Fanex.Data.Repository;

public interface ILetsTalkService
{
    Task SendMessage(int matchId, int userId, string actionName, string message);
}

public class LetsTalkService(IDynamicRepository repository) : ILetsTalkService
{
    public async Task SendMessage(int matchId, int userId, string actionName, string message)
    {
        var criteria = new LetsTalkMessageInsertCriteria
        {
            MatchId = matchId,
            UserId = userId,
            ActionName = actionName,
            MessageContent = message
        };
        await repository.ExecuteAsync(criteria);
    }
}

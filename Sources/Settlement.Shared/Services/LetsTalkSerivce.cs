using Fanex.Data.Repository;

namespace Settlement.Shared.Services;

public interface ILetsTalkService
{
    Task SendMessage(int matchId, int userId, string actionName, string message, int messageGroupId);
}

public class LetsTalkService(IDynamicRepository repository) : ILetsTalkService
{
    public async Task SendMessage(int matchId, int userId, string actionName, string message, int messageGroupId)
    {
        var criteria = new LetsTalkMessageInsertCriteria
        {
            MatchId = matchId,
            UserId = userId,
            ActionName = actionName,
            MessageContent = message,
            MessageGroupId = messageGroupId
        };
        await repository.ExecuteAsync(criteria);
    }
}
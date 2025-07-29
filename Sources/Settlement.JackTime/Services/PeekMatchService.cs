using Settlement.JackTime.Models;

namespace Settlement.JackTime.Services;

internal interface IPeekMatchService
{
    Task<List<Match>> PeekNewMatch();
}

public class PeekMatchService(IJackTimeService jackTimeService) : IPeekMatchService
{
    private readonly List<Match> poolOfMatches = new();

    private readonly object lockObject = new();

    public List<Match> PoolOfMatches
    {
        get
        {
            lock (lockObject)
            {
                return poolOfMatches.ToList();
            }
        }
    }

    public void Remove(Match match)
    {
        lock (lockObject)
        {
            poolOfMatches.RemoveAll(m => m.MatchId == match.MatchId);
        }
    }

    public async Task<List<Match>> PeekNewMatch()
    {
        var matches = await jackTimeService.GetMatches();

        if (matches.Count == 0)
        {
            return [];
        }

        lock (lockObject)
        {
            var newMatches = matches.Except(poolOfMatches).ToList();
            poolOfMatches.AddRange(newMatches);

            return newMatches;
        }
    }
}
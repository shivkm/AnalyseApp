using AnalyseApp.models;

namespace AnalyseApp.Extensions;

internal static class MatchesExtensions
{

    internal static IList<GameData> GetMatchesBy(this IEnumerable<GameData> games, Func<GameData, bool> predicate)
    {
        var currentSession = games
            .Where(predicate)
            .ToList();

        return currentSession;
    }
}
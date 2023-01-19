using AnalyseApp.models;

namespace AnalyseApp.Extensions;

internal static class GameDataExtensions
{

    internal static IList<GameData> GetLeagueSeasonBy(this IEnumerable<GameData> gameData, int startYear, int endYear, string league)
    {
        var startDate = new DateTime(startYear, 08, 01);
        var endDate = new DateTime(endYear, 06, 30);

        var filteredMatches = gameData.Where(i => 
        {
            var matchDate = DateTime.Parse(i.Date);
            return matchDate >= startDate && matchDate <= endDate && i.Div == league;
        }).ToList();

        return filteredMatches;
    }

    // Checking if the given home team and away team are part of the current league.
    internal static bool TeamsAreInLeague(this IList<GameData> gameData, string homeTeam, string awayTeam)
    {
        var teams = gameData.Select(i => i.HomeTeam)
            .Distinct()
            .ToList();

        return teams.Any(i => i == homeTeam) && teams.Any(i => i != awayTeam);
    }
    
    internal static int NumberOfTeamsLeague(this ICollection<GameData> gameData)
    {
        var teams = gameData.Select(i => i.HomeTeam)
            .Distinct()
            .ToList();

        return teams.Count;
    }

    internal static IList<GameData> GetTeamMatchesBy(this IList<GameData> gameData, string team, bool atHome = false)
    {
        var result = gameData
            .Where(i => atHome ? i.HomeTeam == team : i.AwayTeam == team)
            .ToList();

        return result;
    }
    
    
    
    
    internal static IList<GameData> GetMatchesBy(this IEnumerable<GameData> games, Func<GameData, bool> predicate)
    {
        var currentSession = games
            .Where(predicate)
            .ToList();

        return currentSession;
    }
}
using PredictionTool.Models;

namespace PredictionTool.Extensions;

public static class GameExtensions
{
    internal static IEnumerable<Game> GetHistoricalGamesOrderByDateBy(
        this IEnumerable<Game> gameData, DateTime endDate)
    {
        var startDate = new DateTime(2016, 08, 01);
        var filteredMatches = gameData.Where(i => 
            {
                var matchDate = i.DateTime;
                return matchDate >= startDate && matchDate <= endDate;
            })
            .OrderByDescending(i => i.DateTime)
            .ToList();

        return filteredMatches;
    }
    
    internal static List<Game> GetCurrentSeasonGamesBy(this IEnumerable<Game> historicalGames, int startYear, int endYear, string league)
    {
        var startDate = new DateTime(startYear, 08, 01);
        var endDate = new DateTime(endYear, 06, 30);

        var games = historicalGames
            .Where(i => 
            {
                var matchDate = i.DateTime;
                return i.League == league && 
                       matchDate >= startDate &&
                       matchDate <= endDate;
            })
            .OrderByDescending(i => i.DateTime)
            .ToList();

        return games;
    }

    internal static List<Game> GetLastSixGamesBy(this List<Game> games, string team)
    {
        games = games
            .Where(i => i.Home == team || i.Away == team)
            .OrderByDescending(i => i.DateTime)
            .Take(6)
            .ToList();

        return games;
    }

    /// <summary>
    /// Checking if the given home team and away team are part of the current league
    /// </summary>
    /// <param name="gameData">List of historical games</param>
    /// <param name="homeTeam">Home team name</param>
    /// <param name="awayTeam">Away Team name</param>
    /// <returns>True if home and away team exist</returns>
    internal static bool TeamsAreInLeague(this IEnumerable<Game> gameData, string homeTeam, string awayTeam)
    {
        var teams = gameData.Select(i => i.Home)
            .Distinct()
            .ToList();

        return teams.Any(i => i == homeTeam) && teams.Any(i => i != awayTeam);
    }
    
    /// <summary>
    /// Number of team on given league
    /// </summary>
    /// <param name="games"></param>
    /// <returns></returns>
    public static int NumberOfTeamsLeague(this ICollection<Game> games)
    {
        var teams = games.Select(i => i.Home)
            .Distinct()
            .ToList();

        return teams.Count;
    }

    /// <summary>
    /// calculate the accuracy of goal conceded by provided team
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <returns>accuracy of no goal scored by team</returns>
    internal static double CalculateConcededGoalAccuracy(this List<Game> games, string team)
    {
        var homeScoreAverage = games.Where(i => i.Home == team).Sum(i => i.FullTimeAwayScore ?? 0);
        var awayScoreAverage = games.Where(i => i.Away == team).Sum(i => i.FullTimeHomeScore ?? 0);
        var totalGames = games.Count;

        return (homeScoreAverage + awayScoreAverage).Divide(totalGames);
    }
    
    /// <summary>
    /// calculate the accuracy of goal scored by provided team
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <returns>accuracy of no goal scored by team</returns>
    internal static double CalculateScoredGoalAccuracy(this List<Game> games, string team)
    {
        var homeScoreAverage = games.Where(i => i.Home == team).Sum(i => i.FullTimeHomeScore ?? 0);
        var awayScoreAverage = games.Where(i => i.Away == team).Sum(i => i.FullTimeAwayScore ?? 0);
        var totalGames = games.Count;

        return (homeScoreAverage + awayScoreAverage).Divide(totalGames);
    }

    /// <summary>
    /// calculate the accuracy of goal conceded by provided team
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <returns>accuracy of no goal scored by team</returns>
    internal static double CalculateHalftimeScoreGoalAccuracy(this List<Game> games, string team)
    {
        var homeScoreAverage = games.Where(i => i.Home == team).Sum(i => i.HalftimeHomeScore ?? 0);
        var awayScoreAverage = games.Where(i => i.Away == team).Sum(i => i.HalftimeAwayScore ?? 0);
        var totalGames = games.Count;

        return (homeScoreAverage + awayScoreAverage).Divide(totalGames);
    }

    /// <summary>
    /// calculate the accuracy of goal conceded by provided team
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <returns>accuracy of no goal scored by team</returns>
    internal static double CalculateHalftimeConcededGoalAccuracy(this List<Game> games, string team)
    {
        var homeScoreAverage = games.Where(i => i.Home == team).Sum(i => i.HalftimeAwayScore ?? 0);
        var awayScoreAverage = games.Where(i => i.Away == team).Sum(i => i.HalftimeHomeScore ?? 0);
        var totalGames = games.Count;

        return (homeScoreAverage + awayScoreAverage).Divide(totalGames);
    }

    /// <summary>
    /// calculate the accuracy of goal conceded by provided team
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <returns>accuracy of no goal scored by team</returns>
    internal static double CalculateShotsoncededGoalAccuracy(this List<Game> games, string team)
    {
        var homeScoreAverage = games.Where(i => i.Home == team).Sum(i => i.HalftimeAwayScore ?? 0);
        var awayScoreAverage = games.Where(i => i.Away == team).Sum(i => i.HalftimeHomeScore ?? 0);
        var totalGames = games.Count;

        return (homeScoreAverage + awayScoreAverage).Divide(totalGames);
    }
}
using AnalyseApp.Models;

namespace AnalyseApp.Extensions;

internal static class HistoricalGameExtensions
{
    /// <summary>
    /// calculate the accuracy of at least one goal games
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <returns>accuracy of at least one goal by team</returns>
    internal static double CalculateScoreGameAccuracy(this List<HistoricalGame> games, string team)
    {
        var oneGoalGames = games
            .Count(i => i.HomeTeam == team && i.FTHG > 0 || i.AwayTeam == team && i.FTAG > 0)
            .Divide(games.Count);

        return oneGoalGames;
    }

    /// <summary>
    /// calculate the accuracy of halftime scored games
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <returns>accuracy of at least one goal in halftime by team</returns>
    internal static double CalculateHalftimeScoreGamesAccuracy(this List<HistoricalGame> games, string team)
    {
        var zeroZeroGames = games.Count(i => i.HomeTeam == team && i.HTHG > 0 || i.AwayTeam == team && i.HTAG > 0);
        var totalGames = games.Count;

        // Avoid division by zero
        if (totalGames == 0)
            return 0.0; 

        return (double) zeroZeroGames / totalGames;
    }

    /// <summary>
    /// calculate the accuracy of no goal scored by provided team
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="expectedGoals">expected goal</param>
    /// <returns>accuracy of no goal scored by team</returns>
    internal static double CalculateNoScoreGamesAccuracy(this List<HistoricalGame> games, int expectedGoals)
    {
        var zeroZeroGames = games.Count(i =>  i.FTHG == 0 && i.FTAG <= expectedGoals ||
                                                            i.FTAG == 0 && i.FTHG <= expectedGoals);
        var totalGames = games.Count;

        // Avoid division by zero
        if (totalGames == 0)
            return 0.0; 

        return (double) zeroZeroGames / totalGames;
    }
    
    /// <summary>
    /// calculate the accuracy of no goal scored by provided team
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <param name="expectedGoals">expected goal</param>
    /// <returns>accuracy of no goal scored by team</returns>
    internal static double CalculateNoScoreGamesAccuracyBy(this List<HistoricalGame> games, string team, int expectedGoals)
    {
        var zeroZeroGames = games.Count(i => i.HomeTeam == team && i.FTHG == 0 && i.FTAG <= expectedGoals ||
                                             i.AwayTeam == team && i.FTAG == 0 && i.FTHG <= expectedGoals);
        var totalGames = games.Count;

        // Avoid division by zero
        if (totalGames == 0)
            return 0.0; 

        return (double) zeroZeroGames / totalGames;
    }
    
    
    /// <summary>
    /// calculate the accuracy of 0:0 games
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <returns>accuracy of 0:0</returns>
    internal static double CalculateZeroZeroAccuracy(this List<HistoricalGame> games)
    {
        var zeroZeroGames = games.Count(i => i is { FTHG: 0, FTAG: 0 });
        var totalGames = games.Count;

        // Avoid division by zero
        if (totalGames == 0)
            return 0.0; 

        return (double) zeroZeroGames / totalGames;
    }
    
    /// <summary>
    /// calculate the accuracy of no goal scored by provided team
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <returns>accuracy of no goal scored by team</returns>
    internal static double CalculateScoredGoalAccuracy(this List<HistoricalGame> games, string team)
    {
        var homeScoreAverage = games.Where(i => i.HomeTeam == team).Sum(i => i.FTHG);
        var awayScoreAverage = games.Where(i => i.AwayTeam == team).Sum(i => i.FTAG);
        var totalGames = games.Count;

        // Avoid division by zero
        if (totalGames == 0)
            return 0.0; 

        return (homeScoreAverage ?? 0 + awayScoreAverage ?? 0).Divide(totalGames);
    }
    
        
    /// <summary>
    /// calculate the accuracy of goal conceded by provided team
    /// </summary>
    /// <param name="games">List of teams games</param>
    /// <param name="team">team name</param>
    /// <returns>accuracy of no goal scored by team</returns>
    internal static double CalculateConcededGoalAccuracy(this List<HistoricalGame> games, string team)
    {
        var homeScoreAverage = games.Where(i => i.HomeTeam == team).Sum(i => i.FTAG);
        var awayScoreAverage = games.Where(i => i.AwayTeam == team).Sum(i => i.FTHG);
        var totalGames = games.Count;

        // Avoid division by zero
        if (totalGames == 0)
            return 0.0; 

        return (homeScoreAverage ?? 0 + awayScoreAverage ?? 0).Divide(totalGames);
    }
    
    internal static IList<HistoricalGame> GetGameDataBy(this IEnumerable<HistoricalGame> gameData, int startYear, int endYear)
    {
        var startDate = new DateTime(startYear, 08, 01);
        var endDate = new DateTime(endYear, 06, 30);

        var filteredMatches = gameData.Where(i => 
            {
                var matchDate = DateTime.Parse(i.Date);
                return matchDate >= startDate && matchDate <= endDate;
            })
            .OrderByDescending(i => DateTime.Parse(i.Date))
            .ToList();

        return filteredMatches;
    }


    
    internal static List<HistoricalGame> GetLastSixGamesBy(this IEnumerable<HistoricalGame> historicalGames, string team)
    {
        var games = historicalGames
            .Where(i => i.HomeTeam == team || i.AwayTeam == team)
            .GetGameDataBy(2022, 2023)
            .Take(6)
            .ToList();

        return games;
    }
    
    internal static List<HistoricalGame> GetAllSeasonGamesBy(this IEnumerable<HistoricalGame> historicalGames, string team)
    {
        var games = historicalGames
            .Where(i => i.HomeTeam == team || i.AwayTeam == team)
            .GetGameDataBy(2016, 2023)
            .ToList();
        
        return games;
    }
    
    internal static IList<HistoricalGame> GetHeadToHeadGamesBy(this IEnumerable<HistoricalGame> historicalGames, string homeTeam, string awayTeam)
    {
        var games = historicalGames
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                        i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .GetGameDataBy(2016, 2023);
        
        return games;
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    private static double PossibleProbabilities(IList<double> values, IList<double> weights)
    {
        if (values.Count != weights.Count)
            throw new ArgumentException("Values and weights must have the same length.");

        double weightedSum = 0;
        double weightSum = 0;

        for (var i = 0; i < values.Count; i++)
        {
            weightedSum += values[i] * weights[i];
            weightSum += weights[i];
        }

        return weightedSum / weightSum;
    }

    internal static int GetWinGamesCountBy(this IEnumerable<HistoricalGame> currentMatches, string team) =>
        currentMatches.Count(i => i.FTR == "H" && i.HomeTeam == team || i.FTR == "A" && i.AwayTeam == team);
    
    internal static int GetLossGamesCountBy(this IEnumerable<HistoricalGame> currentMatches, string team) =>
        currentMatches.Count(i => i.FTR == "A" && i.HomeTeam == team || i.FTR == "H" && i.AwayTeam == team);

    internal static int GetNoGoalGameCount(this IEnumerable<HistoricalGame> currentMatches) =>
        currentMatches.Count(i => i is { FTHG: 0, FTAG: 0 });

    internal static double GetGoalAverage(this IList<HistoricalGame> pastMatches, string team) =>
        pastMatches.Sum(m => m.HomeTeam == team ? m.FTHG  ?? 0 : m.FTAG ?? 0).Divide(pastMatches.Count);
    
    /// <summary>
    /// Calculate the average goal scored by team
    /// </summary>
    /// <param name="pastMatches">List of past matches</param>
    /// <param name="count">Provide the number of games played by team in current season.
    /// This will be needed if you calculate the average of team scored in current season.</param>
    /// <param name="atHome">This will give the average score of the team played at home</param>
    /// <returns></returns>
    internal static double GetGoalScoreAverage(this IList<HistoricalGame> pastMatches, int count = 0, bool atHome = false)
    {
        var countValue = count > 0 ? count * pastMatches.NumberOfTeamsLeague() : pastMatches.Count;

        var totalGoals = atHome 
            ? pastMatches
                .Sum(m => m.FTHG ?? 0)
            
            : pastMatches
                .Sum(m => m.FTAG ?? 0);

        return totalGoals.Divide(countValue);
    }
    
    /// <summary>
    /// Calculate the average goal conceded
    /// </summary>
    /// <param name="pastMatches">List of past matches of the team or league</param>
    /// <param name="count">Provide the number of games played by team in current season.
    /// This will be needed if you calculate the average of team scored in current season.</param>
    /// <param name="atHome">This will give the average score of the team played at home</param>
    /// <returns></returns>
    internal static double GetGoalConcededAverage(this IList<HistoricalGame> pastMatches,
        int count = 0, bool atHome = false)
    {
        var countValue = count > 0 ? count * pastMatches.NumberOfTeamsLeague() : pastMatches.Count;

        var totalGoals = atHome 
            ? pastMatches
                .Sum(m => m.FTAG ?? 0)
            
            : pastMatches
                .Sum(m => m.FTHG ?? 0);

        return totalGoals.Divide(countValue);
    }
    

    internal static double GetGoalGameAverage(this IList<HistoricalGame> pastMatches) =>
        pastMatches.Count(m => m is { FTHG: > 0, FTAG: > 0 }).Divide(pastMatches.Count);

    internal static double GetNoGoalGameAverage(this IList<HistoricalGame> pastMatches) =>
        pastMatches.Count(i => i is { FTHG: 0, FTAG: 0 }).Divide(pastMatches.Count);

    internal static int GetShotOnGoalsCountBy(this IEnumerable<HistoricalGame> currentMatches, string team) =>
        currentMatches.Count(i => i.HST > 0 && i.HomeTeam == team || i.AST > 0 && i.AwayTeam == team);

    internal static int GetOffSideCountBy(this IEnumerable<HistoricalGame> currentMatches, string team) =>
        currentMatches.Count(i => i.HO > 0 && i.HomeTeam == team || i.AO > 0 && i.AwayTeam == team);

    internal static int GetFoulCommittedCountBy(this IEnumerable<HistoricalGame> currentMatches, string team) =>
        currentMatches.Count(i =>i.HF > 0 && i.HomeTeam == team || i.AF > 0 && i.AwayTeam == team);

    internal static int GetScoredGoalGames(this IEnumerable<HistoricalGame> currentMatches, string team) =>
        currentMatches.Count(i => i.HomeTeam == team && i.FTHG > 0 || i.AwayTeam == team && i.FTAG > 0);

    internal static int GetGoalConcededGames(this IEnumerable<HistoricalGame> currentMatches, string team) =>
        currentMatches.Count(i => i.HomeTeam == team && i.FTAG > 0 || i.AwayTeam == team && i.FTHG > 0);

    internal static int GetOneSideGoalGamesCount(this IEnumerable<HistoricalGame> currentMatches) =>
        currentMatches.Count(i => i is { FTHG: > 0, FTAG: 0 } or { FTHG: 0, FTAG: > 0 });
    
    internal static int GetBothScoredGamesCount(this IEnumerable<HistoricalGame> currentMatches) =>
        currentMatches.Count(i => i is { FTHG: > 0, FTAG: > 0 });
    
    internal static int GetMoreThanTwoGoalScoredGamesCount(this IEnumerable<HistoricalGame> currentMatches) =>
        currentMatches.Count(i => i.FTHG + i.FTAG > 2);
    
    internal static int GetTwoToThreeGoalScoredGamesCount(this IEnumerable<HistoricalGame> currentMatches) =>
        currentMatches.Count(i => i.FTHG + i.FTAG == 2 || i.FTHG + i.FTAG == 3);
    
    internal static int GetHalftimeGoalScoredSumBy(this IList<HistoricalGame> currentMatches, string team) =>
        currentMatches.Where(i => i.HomeTeam == team).Sum(i => i.HTHG ?? 0) +
        currentMatches.Where(i => i.AwayTeam == team).Sum(i => i.HTAG ?? 0);
    
    internal static int GetHalftimeGoalConcededSumBy(this IList<HistoricalGame> currentMatches, string team) =>
        currentMatches.Where(i => i.HomeTeam == team).Sum(i => i.HTAG ?? 0) +
        currentMatches.Where(i => i.AwayTeam == team).Sum(i => i.HTHG ?? 0);
    
    internal static int GetHalftimeGoalGamesCount(this IEnumerable<HistoricalGame> currentMatches) =>
        currentMatches.Count(i => i.HTHG > 0 || i.HTAG > 0);

    
    // Checking if the given home team and away team are part of the current league.
    internal static bool TeamsAreInLeague(this IList<HistoricalGame> gameData, string homeTeam, string awayTeam)
    {
        var teams = gameData.Select(i => i.HomeTeam)
            .Distinct()
            .ToList();

        return teams.Any(i => i == homeTeam) && teams.Any(i => i != awayTeam);
    }

    
    internal static int NumberOfTeamsLeague(this ICollection<HistoricalGame> gameData)
    {
        var teams = gameData.Select(i => i.HomeTeam)
            .Distinct()
            .ToList();

        return teams.Count;
    }
}
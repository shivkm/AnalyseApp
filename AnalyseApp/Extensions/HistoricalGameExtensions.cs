using AnalyseApp.Models;

namespace AnalyseApp.Extensions;

internal static class HistoricalGameExtensions
{
    
    internal static IList<HistoricalGame> GetGameDataBy(
        this IEnumerable<HistoricalGame> gameData, int startYear, int endYear)
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

    private static double WeightedAverage(IList<double> values, IList<double> weights)
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
    
    internal static HeadToHead GetHeadToHeadGamesBy(this IList<HistoricalGame> historicalGames, string homeTeam, string awayTeam)
    {
        // Get all head to head matches
        var pastMatches = historicalGames
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                                    i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .GetGameDataBy(2016, 2023);

        // check the last two matches wasn't 0:0 and the last match is not older than one year
        var lastTwoZeroZeroGames = pastMatches
            .OrderByDescending(i => i.Date).Take(2)
            .Any(i => i is { FTHG: 0, FTAG: 0 } && DateTime.Parse(i.Date) > DateTime.Now.AddYears(-1));
        
        var homeGoalAverage = pastMatches
            .Sum(m => m.HomeTeam == homeTeam ? m.FTHG  ?? 0 : m.FTAG ?? 0)
            .Divide(pastMatches.Count);
        
        var homeHalftimeGoalAverage = pastMatches
            .Sum(m => m.HomeTeam == homeTeam ? m.HTHG  ?? 0 : m.HTAG ?? 0)
            .Divide(pastMatches.Count);
        
        var homeShotOnGoalsAverage = pastMatches
            .Sum(m => m.HomeTeam == homeTeam ? m.HST  ?? 0 : m.AST ?? 0)
            .Divide(pastMatches.Count);
        
        var homeShotAverage = pastMatches
            .Sum(m => m.HomeTeam == homeTeam ? m.HS  ?? 0 : m.AS ?? 0)
            .Divide(pastMatches.Count);

        var homeShotAccuracy = homeShotOnGoalsAverage / homeGoalAverage;

        var awayGoalAverage = pastMatches
            .Sum(m => m.HomeTeam == awayTeam ? m.FTHG  ?? 0 : m.FTAG ?? 0)
            .Divide(pastMatches.Count);
        
        var awayShotOnGoalsAverage = pastMatches
            .Sum(m => m.HomeTeam == awayTeam ? m.HST  ?? 0 : m.AST ?? 0)
            .Divide(pastMatches.Count);
        
        var awayHalftimeGoalAverage = pastMatches
            .Sum(m => m.HomeTeam == awayTeam ? m.HTHG  ?? 0 : m.HTAG ?? 0)
            .Divide(pastMatches.Count);
        
        var awayShotAverage = pastMatches
            .Sum(m => m.HomeTeam == awayTeam ? m.HS  ?? 0 : m.AS ?? 0)
            .Divide(pastMatches.Count);
        
        var awaySotAccuracy = awayShotOnGoalsAverage / awayShotAverage;
        // Use Naive Bayes algorithm to predict the average scores
        var homeWins = pastMatches.Count(m => m.HomeTeam == homeTeam ? m.FTR == "H" : m.FTR == "A");
        var awayWins = pastMatches.Count(m => m.HomeTeam == awayTeam ? m.FTR == "H" : m.FTR == "A");
        var draws = pastMatches.Count(m => m.FTR == "D");

        var headToHead = new HeadToHead
        {
            GamesPlayed = pastMatches.Count,
            HomeScoreAverage = homeGoalAverage + homeWins * homeGoalAverage + 
                               homeHalftimeGoalAverage * homeGoalAverage +
                               homeShotAccuracy * homeGoalAverage,
            
            AwayScoreAverage = awayGoalAverage + awayWins * awayGoalAverage + 
                               awayHalftimeGoalAverage * awayGoalAverage +
                               awaySotAccuracy * awayGoalAverage,
            
            LastTwoZeroZeroGames = lastTwoZeroZeroGames
        };
        
        return headToHead;
    }
    
    internal static TeamData GetCurrentSeasonGamesBy(this IList<HistoricalGame> historicalGames, string team)
    {
        var homeGames = historicalGames
            .Where(i => i.HomeTeam == team)
            .GetGameDataBy(2022, 2023);
        
        var awayGames = historicalGames
            .Where(i => i.AwayTeam == team)
            .GetGameDataBy(2022, 2023);

        var teamData = GetTeamData(homeGames.Take(4).ToList(), awayGames.Take(4).ToList(), team);
        
        return teamData;
    }
    
    internal static TeamData GetAllSeasonGamesBy(this IList<HistoricalGame> historicalGames, string team)
    {
        var homeGames = historicalGames
            .Where(i => i.HomeTeam == team)
            .GetGameDataBy(2016, 2022);
        
        var awayGames = historicalGames
            .Where(i => i.AwayTeam == team)
            .GetGameDataBy(2016, 2022);

        var teamData = GetTeamData(homeGames, awayGames, team);

        return teamData;
    }

    private static TeamData GetTeamData(ICollection<HistoricalGame> homeMatches, IList<HistoricalGame> awayMatches, string team)
    {
        var lastHomeNoGoalGame = homeMatches.OrderByDescending(i => i.Date).Take(1).Any(i => i is { FTHG: 0, FTAG: 0 });
        var homeZeroGoalAverage = homeMatches.Count(i => i.FTHG == 0).Divide(homeMatches.Count);
        var homeOneGoalAverage = homeMatches.Count(i => i.FTHG > 0).Divide(homeMatches.Count);
        var homeHalftimeOneGoalAverage = homeMatches.Count(i => i.HTHG > 0).Divide(homeMatches.Count);
        var homeHalftimeConcededAverage = homeMatches.Count(i => i.HTAG > 0).Divide(homeMatches.Count);
        var homeGoalConcededAverage = homeMatches.Count(i => i.FTAG > 0).Divide(homeMatches.Count);
        
        var lastAwayNoGoalGame = awayMatches.OrderByDescending(i => i.Date).Take(1).Any(i => i is { FTHG: 0, FTAG: 0 });
        var awayZeroGoalAverage = awayMatches.Count(i => i.FTAG == 0).Divide(homeMatches.Count);
        var awayOneGoalAverage = awayMatches.Count(i => i.FTAG > 0).Divide(homeMatches.Count);
        var awayHalftimeOneGoalAverage = awayMatches.Count(i => i.FTAG > 0).Divide(homeMatches.Count);
        var awayGoalConcededAverage = awayMatches.Count(i => i.FTHG > 0).Divide(homeMatches.Count);
        var awayHalftimeConcededAverage = awayMatches.Count(i => i.HTHG > 0).Divide(homeMatches.Count);

        var goalAverage = homeOneGoalAverage * 0.50 + awayOneGoalAverage * 0.50;
        var halftimeGoalAverage = homeHalftimeOneGoalAverage * 0.50 + awayHalftimeOneGoalAverage * 0.50;
        var concededAverage = homeGoalConcededAverage * 0.50 + awayGoalConcededAverage * 0.50;
        var halftimeConcededAverage = homeHalftimeConcededAverage * 0.50 + awayHalftimeConcededAverage * 0.50;
        var zeroGaolAverage = homeZeroGoalAverage * 0.50 + awayZeroGoalAverage * 0.50;

        var teamData = new TeamData
        {
            Team = team,
            GoalAverage = goalAverage,
            OneGoalQualified = goalAverage > 0.58,
            ConcededAverage = concededAverage,
            ConcededQualified = concededAverage > 0.58,
            HalftimeGoalAverage = halftimeGoalAverage,
            HalftimeConcededAverage = halftimeConcededAverage,
            HalftimeOneGoalQualified = halftimeGoalAverage > 0.68,
            ZeroGoalAverage = zeroGaolAverage,
            LastHomeGameZeroZero = lastHomeNoGoalGame,
            LastAwayGameZeroZero = lastAwayNoGoalGame
        };

        return teamData;
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
    /// <param name="team">The current team for calculating the average</param>
    /// <param name="count">Provide the number of games played by team in current season.
    /// This will be needed if you calculate the average of team scored in current season.</param>
    /// <param name="atHome">This will give the average score of the team played at home</param>
    /// <param name="general">This will give the average score of the team played at away</param>
    /// <returns></returns>
    internal static double GetGoalScoreAverage(this IList<HistoricalGame> pastMatches, 
        string team, int count = 0, bool atHome = false, bool general = false)
    {
        var countValue = count > 0 ? count * pastMatches.NumberOfTeamsLeague() : pastMatches.Count;

        var totalGoals = atHome 
            ? pastMatches
                .Where(i => i.HomeTeam == team)
                .Sum(m => m.FTHG ?? 0)
            
            : pastMatches
                .Where(i => i.AwayTeam == team)
                .Sum(m => m.FTAG ?? 0);

        if (!general) return totalGoals.Divide(countValue);
        
        var homeGoalSum = pastMatches.Where(i => i.HomeTeam == team).Sum(m => m.FTHG ?? 0);
        var awayGoalSum = pastMatches.Where(i => i.AwayTeam == team).Sum(m => m.FTAG ?? 0);

        totalGoals = homeGoalSum + awayGoalSum;

        return totalGoals.Divide(countValue);
    }
    
    /// <summary>
    /// Calculate the average goal scored by team
    /// </summary>
    /// <param name="pastMatches">List of past matches</param>
    /// <param name="team">The current team for calculating the average</param>
    /// <param name="count">Provide the number of games played by team in current season.
    /// This will be needed if you calculate the average of team scored in current season.</param>
    /// <param name="atHome">This will give the average score of the team played at home</param>
    /// <param name="general">This will give the average score of the team played at away</param>
    /// <returns></returns>
    internal static double GetGoalConcededAverage(this IList<HistoricalGame> pastMatches, 
        string team, int count = 0, bool atHome = false, bool general = false)
    {
        var countValue = count > 0 ? count * pastMatches.NumberOfTeamsLeague() : pastMatches.Count;

        var totalGoals = atHome 
            ? pastMatches
                .Where(i => i.HomeTeam == team)
                .Sum(m => m.FTAG ?? 0)
            
            : pastMatches
                .Where(i => i.AwayTeam == team)
                .Sum(m => m.FTHG ?? 0);

        if (!general) return totalGoals.Divide(countValue);
        
        var homeGoalSum = pastMatches.Where(i => i.HomeTeam == team).Sum(m => m.FTAG ?? 0);
        var awayGoalSum = pastMatches.Where(i => i.AwayTeam == team).Sum(m => m.FTHG ?? 0);

        totalGoals = homeGoalSum + awayGoalSum;

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
    
    internal static int GetHalftimeGoalScoredGamesCount(this IEnumerable<HistoricalGame> currentMatches) =>
        currentMatches.Count(i => i.HTHG > 0 || i.HTAG > 0);

    
    // Checking if the given home team and away team are part of the current league.
    internal static bool TeamsAreInLeague(this IList<HistoricalGame> gameData, string homeTeam, string awayTeam)
    {
        var teams = gameData.Select(i => i.HomeTeam)
            .Distinct()
            .ToList();

        return teams.Any(i => i == homeTeam) && teams.Any(i => i != awayTeam);
    }
    
    // Checking if the given home team and away team are part of the current league.
    internal static bool TeamIsInLeague(this IEnumerable<HistoricalGame> gameData, string team)
    {
        var teams = gameData.Select(i => i.HomeTeam)
            .Distinct()
            .ToList();

        return teams.Any(i => i == team);
    }

    
    internal static int NumberOfTeamsLeague(this ICollection<HistoricalGame> gameData)
    {
        var teams = gameData.Select(i => i.HomeTeam)
            .Distinct()
            .ToList();

        return teams.Count;
    }

    internal static IList<HistoricalGame> GetTeamMatchesBy(this IList<HistoricalGame> gameData, string team, bool atHome = false)
    {
        var result = gameData
            .Where(i => atHome ? i.HomeTeam == team : i.AwayTeam == team)
            .ToList();

        return result;
    }
    
    
    
    
    internal static IList<HistoricalGame> GetMatchesBy(this IEnumerable<HistoricalGame> games, Func<HistoricalGame, bool> predicate)
    {
        var currentSession = games
            .Where(predicate)
            .ToList();

        return currentSession;
    }
}
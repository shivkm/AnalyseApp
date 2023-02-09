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

    internal static HeadToHead GetHeadToHeadGamesBy(this IList<HistoricalGame> historicalGames, string homeTeam, string awayTeam)
    {
        var homeGames = historicalGames
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam)
            .GetGameDataBy(2016, 2023);
        
        var awayGames = historicalGames
            .Where(i => i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .GetGameDataBy(2016, 2023);

        var lastHomeNoGoalGame = homeGames
            .OrderByDescending(i => i.Date).Take(2)
            .Any(i => i is { FTHG: 0, FTAG: 0 });
        
        var homeZeroGoalAverage = homeGames.Count(i => i.FTHG + i.FTAG == 0).Divide(homeGames.Count);
        var bothScored = homeGames.Count(i => i is { FTHG: > 0, FTAG: > 0 }).Divide(homeGames.Count);
        var twoToThreeGoal = homeGames.Count(i => i.FTHG + i.FTAG == 2 || i.FTHG + i.FTAG == 3).Divide(homeGames.Count);
        var moreThanTwoGoals = homeGames.Count(i => i.FTHG + i.FTAG > 2).Divide(homeGames.Count);
        var halftimeScoredGoal = homeGames.Count(i => i.HTAG + i.HTHG > 0).Divide(homeGames.Count);
        var oneSideScore = homeGames.Count(i => i is { FTHG: > 0, FTAG: 0 } or { FTHG: 0, FTAG: > 0 }).Divide(homeGames.Count);
        var lessThanThreeGoals = homeGames.Count(i => i.FTHG + i.FTAG < 3).Divide(homeGames.Count);
        
        var lastAwayNoGoalGame = awayGames
            .OrderByDescending(i => i.Date).Take(2)
            .Any(i => i is { FTHG: 0, FTAG: 0 });
        
        var awayZeroGoalAverage = awayGames.Count(i => i.FTHG + i.FTAG == 0).Divide(awayGames.Count);
        var awayBothScored = awayGames.Count(i => i is { FTHG: > 0, FTAG: > 0 }).Divide(awayGames.Count);
        var awayTwoToThreeGoal = awayGames.Count(i => i.FTHG + i.FTAG == 2 || i.FTHG + i.FTAG == 3).Divide(awayGames.Count);
        var awayMoreThanTwoGoals = awayGames.Count(i => i.FTHG + i.FTAG > 2).Divide(awayGames.Count);
        var awayHalftimeScoredGoal = awayGames.Count(i => i.HTAG + i.HTHG > 0).Divide(awayGames.Count);
        var awayOneSideScore = awayGames.Count(i => i is { FTHG: > 0, FTAG: 0 } or { FTHG: 0, FTAG: > 0 }).Divide(awayGames.Count);
        var awayLessThanThreeGoals = homeGames.Count(i => i.FTHG + i.FTAG < 3).Divide(homeGames.Count);

        var headToHead = new HeadToHead
        {
            GamesPlayed = homeGames.Count + awayGames.Count,
            BothTeamScored = bothScored * 0.50 + awayBothScored * 0.50,
            TwoToThreeScored = twoToThreeGoal * 0.50 + awayTwoToThreeGoal * 0.50,
            MoreThanTwoScored = moreThanTwoGoals * 0.50 + awayMoreThanTwoGoals * 0.50,
            HalfTimeScored = halftimeScoredGoal * 0.50 + awayHalftimeScoredGoal * 0.50,
            NoScored = homeZeroGoalAverage * 0.50 + awayZeroGoalAverage * 0.50,
            LastHomeGameZeroZero = lastHomeNoGoalGame,
            LastAwayGameZeroZero = lastAwayNoGoalGame,
            AwaySideScored = oneSideScore * 0.50 + awayOneSideScore * 0.50,
            LessThanThreeGoal = lessThanThreeGoals * 0.50 + awayLessThanThreeGoals * 0.50,
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

    internal static int GetShotsCountBy(this IEnumerable<HistoricalGame> currentMatches, string team) =>
        currentMatches.Count(i => i.HS > 0 && i.HomeTeam == team || i.AS > 0 && i.AwayTeam == team);

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
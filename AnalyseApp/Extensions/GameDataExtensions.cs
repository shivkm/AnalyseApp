using AnalyseApp.Models;

namespace AnalyseApp.Extensions;

internal static class GameDataExtensions
{
    
    internal static IList<GameData> GetGameDataBy(
        this IEnumerable<GameData> gameData, int startYear, int endYear)
    {
        var startDate = new DateTime(startYear, 08, 01);
        var endDate = new DateTime(endYear, 06, 30);

        var filteredMatches = gameData.Where(i => 
        {
            var matchDate = DateTime.Parse(i.Date);
            return matchDate >= startDate && matchDate <= endDate;
        }).OrderByDescending(i => DateTime.Parse(i.Date)).ToList();

        return filteredMatches;
    }
    
    internal static int GetWinGamesCountBy(this IEnumerable<GameData> currentMatches, string team) =>
        currentMatches.Count(i => i.FTR == "H" && i.HomeTeam == team || i.FTR == "A" && i.AwayTeam == team);
    
    internal static int GetLossGamesCountBy(this IEnumerable<GameData> currentMatches, string team) =>
        currentMatches.Count(i => i.FTR == "A" && i.HomeTeam == team || i.FTR == "H" && i.AwayTeam == team);

    internal static int GetNoGoalGameCount(this IEnumerable<GameData> currentMatches) =>
        currentMatches.Count(i => i is { FTHG: 0, FTAG: 0 });

    internal static int GetShotsCountBy(this IEnumerable<GameData> currentMatches, string team) =>
        currentMatches.Count(i => i.HS > 0 && i.HomeTeam == team || i.AS > 0 && i.AwayTeam == team);

    internal static int GetShotOnGoalsCountBy(this IEnumerable<GameData> currentMatches, string team) =>
        currentMatches.Count(i => i.HST > 0 && i.HomeTeam == team || i.AST > 0 && i.AwayTeam == team);

    internal static int GetOffSideCountBy(this IEnumerable<GameData> currentMatches, string team) =>
        currentMatches.Count(i => i.HO > 0 && i.HomeTeam == team || i.AO > 0 && i.AwayTeam == team);

    internal static int GetFoulCommittedCountBy(this IEnumerable<GameData> currentMatches, string team) =>
        currentMatches.Count(i =>i.HF > 0 && i.HomeTeam == team || i.AF > 0 && i.AwayTeam == team);

    internal static int GetGoalScoredSumBy(this IList<GameData> currentMatches, string team) =>
        currentMatches.Where(i => i.HomeTeam == team).Sum(i => i.FTHG ?? 0) +
        currentMatches.Where(i => i.AwayTeam == team).Sum(i => i.FTAG ?? 0);

    internal static int GetGoalConcededSumBy(this IList<GameData> currentMatches, string team) =>
        currentMatches.Where(i => i.HomeTeam == team).Sum(i => i.FTAG ?? 0) +
        currentMatches.Where(i => i.AwayTeam == team).Sum(i => i.FTHG ?? 0);

    internal static int GetOneSideGoalGamesCount(this IEnumerable<GameData> currentMatches) =>
        currentMatches.Count(i => i is { FTHG: > 0, FTAG: 0 } or { FTHG: 0, FTAG: > 0 });
    
    internal static int GetBothScoredGamesCount(this IEnumerable<GameData> currentMatches) =>
        currentMatches.Count(i => i is { FTHG: > 0, FTAG: > 0 });
    
    internal static int GetMoreThanTwoGoalScoredGamesCount(this IEnumerable<GameData> currentMatches) =>
        currentMatches.Count(i => i.FTHG + i.FTAG > 2);
    
    internal static int GetTwoToThreeGoalScoredGamesCount(this IEnumerable<GameData> currentMatches) =>
        currentMatches.Count(i => i.FTHG + i.FTAG == 2 || i.FTHG + i.FTAG == 3);
    
    internal static int GetHalftimeGoalScoredSumBy(this IList<GameData> currentMatches, string team) =>
        currentMatches.Where(i => i.HomeTeam == team).Sum(i => i.HTHG ?? 0) +
        currentMatches.Where(i => i.AwayTeam == team).Sum(i => i.HTAG ?? 0);
    
    internal static int GetHalftimeGoalConcededSumBy(this IList<GameData> currentMatches, string team) =>
        currentMatches.Where(i => i.HomeTeam == team).Sum(i => i.HTAG ?? 0) +
        currentMatches.Where(i => i.AwayTeam == team).Sum(i => i.HTHG ?? 0);
    
    internal static int GetHalftimeGoalScoredGamesCount(this IEnumerable<GameData> currentMatches) =>
        currentMatches.Count(i => i.HTHG > 0 || i.HTAG > 0);

    
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
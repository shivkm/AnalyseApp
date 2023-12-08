using AnalyseApp.Models;

namespace AnalyseApp.Extensions;

public static class DataProcessorExtensions
{
    public static IList<Match> GetMatchesBy(this IEnumerable<Match> games, Func<Match, bool> predicate)
    {
        var currentSession = games
            .Where(predicate)
            .ToList();

        return currentSession;
    } 
    
    internal static IEnumerable<Match> GetCurrentLeagueBy(this List<Match> matches, int currentSeasonYear)
    {
        var formatStartDate = $"20/07/{currentSeasonYear}";
        var foundMatches = matches
            .Where(i =>
            {
                var matchDate = i.Date.Parse();
                return matchDate > formatStartDate.Parse();
            })
            .OrderByDescending(i => i.Date.Parse())
            .ToList();

        return foundMatches;
    } 
    
    public static float GetWinAverageRate(this IEnumerable<Match> matches, string teamName)
    {
        var winMatches = matches
            .Count(m => m.HomeTeam == teamName && m.FullTimeHomeGoals > m.FullTimeAwayGoals || 
                        m.AwayTeam == teamName && m.FullTimeAwayGoals > m.FullTimeHomeGoals)
            ;

        return winMatches / (float)matches.Count();
    }
    
    public static float GetGoalAverageRate(this IEnumerable<Match> matches, string teamName, bool halfTime = false)
    {
        var totalGoals = matches
                             .Where(m => m.HomeTeam == teamName)
                             .Sum(m => m.FullTimeHomeGoals) + 
                         matches
                             .Where(m => m.AwayTeam == teamName)
                             .Sum(m => m.FullTimeAwayGoals);

        if (halfTime)
        {
            totalGoals = matches
                             .Where(m => m.HomeTeam == teamName)
                             .Sum(m => m.HalfTimeHomeGoals) + 
                         matches
                             .Where(m => m.AwayTeam == teamName)
                             .Sum(m => m.HalfTimeAwayGoals);
        }
        
        var totalMatches = matches
            .Count(m => m.HomeTeam == teamName || m.AwayTeam == teamName);

        return totalMatches > 0 ? totalGoals / totalMatches : 0;
    }
    
    public static float GetShotAverageRate(this IEnumerable<Match> matches, string teamName, bool targetShots = false)
    {
        var totalShots = matches
                             .Where(m => m.HomeTeam == teamName)
                             .Sum(m => m.HomeShots) + 
                         matches
                             .Where(m => m.AwayTeam == teamName)
                             .Sum(m => m.AwayShots);

        if (targetShots)
        {
            totalShots = matches
                             .Where(m => m.HomeTeam == teamName)
                             .Sum(m => m.HomeTargetShots) + 
                         matches
                             .Where(m => m.AwayTeam == teamName)
                             .Sum(m => m.AwayTargetShots);
        }
        
        var totalMatches = matches
            .Count(m => m.HomeTeam == teamName || m.AwayTeam == teamName);

        return totalMatches > 0 ? totalShots / totalMatches : 0;
    }
    
    public static float GetGoalPerMatchAverageRate(this IEnumerable<Match> matches)
    {
        var totalGoals = matches
            .Sum(m => m.FullTimeHomeGoals + m.FullTimeAwayGoals);

        return totalGoals / matches.Count();
    }    
    
    public static float GetOverTwoGoalsMatchAverageRate(this IEnumerable<Match> matches)
    {
        var totalGoals = matches
            .Count(m => m.FullTimeHomeGoals + m.FullTimeAwayGoals > 2);

        return totalGoals / (float)matches.Count();
    }

    public static MatchData GetMatchDataBy(this TeamData homeTeamData, TeamData awayTeamData, DateTime matchDate)
    {
        var matchData = new MatchData
        {
            Date = matchDate,
            Home = homeTeamData.TeamName,
            HomeScoredGoalsAverage = homeTeamData.ScoredGoalsAverage,
            HomeConcededGoalsAverage = homeTeamData.ConcededGoalsAverage,
            HomeHalfTimeScoredGoalsAverage = homeTeamData.HalfTimeScoredGoalAverage,
            HomeHalfTimeConcededGoalsAverage = homeTeamData.HalfTimeConcededGoalAverage,
            HomeScoredShotsAverage = homeTeamData.ScoredShotsAverage,
            HomeConcededShotsAverage = homeTeamData.ConcededShotsAverage,
            HomeScoredTargetShotsAverage = homeTeamData.ScoredTargetShotsAverage,
            HomeConcededTargetShotsAverage = homeTeamData.ConcededTargetShotsAverage,

            Away = awayTeamData.TeamName,
            AwayScoredGoalsAverage = awayTeamData.ScoredGoalsAverage,
            AwayConcededGoalsAverage = awayTeamData.ConcededGoalsAverage,
            AwayHalfTimeScoredGoalsAverage = awayTeamData.HalfTimeScoredGoalAverage,
            AwayHalfTimeConcededGoalsAverage = awayTeamData.HalfTimeConcededGoalAverage,
            AwayScoredShotsAverage = awayTeamData.ScoredShotsAverage,
            AwayConcededShotsAverage = awayTeamData.ConcededShotsAverage,
            AwayScoredTargetShotsAverage = awayTeamData.ScoredTargetShotsAverage,
            AwayConcededTargetShotsAverage = awayTeamData.ConcededTargetShotsAverage,

            OverUnderTwoGoals = true,
            BothTeamsScored = true,
            TwoToThreeGoals = false
        };


        return matchData;
    }
}
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
    
    internal static IEnumerable<Match> GetHistoricalMatchesOlderThen(this List<Match> matches, DateTime playedOn)
    {
        var foundMatches = matches
            .Where(i =>
            {
                var matchDate = i.Date.Parse();
                return matchDate < playedOn;
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
    
    public static double GetAverageBy(
        this IEnumerable<Match> matches, 
        bool zeroZeroGoals = false, 
        bool overTwoGoals = false, 
        bool goalGoal = false
    )
    {
        var countMatches = matches.Count(m => m.FullTimeHomeGoals + m.FullTimeAwayGoals < 3);

        if (zeroZeroGoals)
            countMatches = matches.Count(m => m is { FullTimeHomeGoals: 0, FullTimeAwayGoals: 0 });
        
        if (overTwoGoals)
            countMatches = matches.Count(m => m.FullTimeHomeGoals + m.FullTimeAwayGoals > 2);
        
        if (goalGoal)
            countMatches = matches.Count(m => m is { FullTimeHomeGoals: > 0, FullTimeAwayGoals: > 0 });
        
        var average = (double)countMatches / matches.Count();
        return average;
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
            HomeZeroZeroMatchAverage = homeTeamData.ZeroZeroMatchAverage,
            HomeUnderThreeGoalsMatchAverage = homeTeamData.UnderThreeGoalsMatchAverage,
            HomeOverTwoGoalsMatchAverage = homeTeamData.OverTwoGoalsMatchAverage,
            HomeGoalGoalMatchAverage = homeTeamData.GoalGoalsMatchAverage,
            
            Away = awayTeamData.TeamName,
            AwayScoredGoalsAverage = awayTeamData.ScoredGoalsAverage,
            AwayConcededGoalsAverage = awayTeamData.ConcededGoalsAverage,
            AwayHalfTimeScoredGoalsAverage = awayTeamData.HalfTimeScoredGoalAverage,
            AwayHalfTimeConcededGoalsAverage = awayTeamData.HalfTimeConcededGoalAverage,
            AwayZeroZeroMatchAverage = awayTeamData.ZeroZeroMatchAverage,
            AwayUnderThreeGoalsMatchAverage = awayTeamData.UnderThreeGoalsMatchAverage,
            AwayOverTwoGoalsMatchAverage =awayTeamData.OverTwoGoalsMatchAverage,
            AwayGoalGoalMatchAverage = awayTeamData.GoalGoalsMatchAverage,
            
            OverUnderTwoGoals = false,
            BothTeamsScored = false,
            TwoToThreeGoals = false, AwayWin = false, HomeWin = false
        };


        return matchData;
    }
}
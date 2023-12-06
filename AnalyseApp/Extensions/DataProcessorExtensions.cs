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
    
    public static float GetWinAverageRate(this IEnumerable<Match> matches, string teamName)
    {
        var winMatches = matches
            .Count(m => m.HomeTeam == teamName && m.FullTimeHomeGoals > m.FullTimeAwayGoals || 
                        m.AwayTeam == teamName && m.FullTimeAwayGoals > m.FullTimeHomeGoals)
            ;

        return winMatches / (float)matches.Count();
    }
    
    public static float GetGoalAverageRate(this IEnumerable<Match> matches, string teamName)
    {
        var goals = matches
                .Count(m => m.HomeTeam == teamName && m.FullTimeHomeGoals > 0 || 
                            m.AwayTeam == teamName && m.FullTimeAwayGoals > 0)
            ;

        return goals / (float)matches.Count();
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
}
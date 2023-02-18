using AnalyseApp.Extensions;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class HalftimeGoalHandler : AnalyseHandler
{
    public override GameQualification HandleRequest(List<HistoricalGame> pastGames, GameQualification gameQualification, string homeTeam, string awayTeam)
    {
        var homeTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();
        
        var awayTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();

        gameQualification.Home.HalfTimeProbability = GetOneGoalProbabilityBy(homeTeamPastMatches);
        gameQualification.Home.HalftimeScoreAverage = homeTeamPastMatches
            .Count(i => (i.HTHG ?? 0) > 0 || (i.HTAG ?? 0) > 0)
            .Divide(homeTeamPastMatches.Count);
        
        gameQualification.Away.HalfTimeProbability = GetOneGoalProbabilityBy(awayTeamPastMatches);
        gameQualification.Away.HalftimeScoreAverage = awayTeamPastMatches
            .Count(i => (i.HTHG ?? 0) > 0 || (i.HTAG ?? 0) > 0)
            .Divide(awayTeamPastMatches.Count);
        
        base.HandleRequest(pastGames, gameQualification, homeTeam, awayTeam);

        return gameQualification;
    }

    private static double GetOneGoalProbabilityBy(IReadOnlyCollection<HistoricalGame> pastMatches)
    {
        var halftimeGoalsScored = pastMatches.Sum(i => i.HTHG) ?? 0;
        var halftimeGoalsConceded = pastMatches.Sum(i => i.HTAG) ?? 0;

        var average = (halftimeGoalsScored + halftimeGoalsConceded).Divide(pastMatches.Count);
        var probability = GetProbabilityBy(average, 1);
        
        return probability;
    }
}
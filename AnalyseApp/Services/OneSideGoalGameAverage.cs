using AnalyseApp.Extensions;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class OneSideGoalGameAverage: AnalyseHandler
{
    public override GameQualification HandleRequest(List<HistoricalGame> pastGames, GameQualification gameQualification, string homeTeam, string awayTeam)
    {
        var homeTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();
        
        var awayTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();
        
        // One side goal games for more than two goals
        gameQualification.Home.OneSideMoreThanTwoGoalGameAverage = OneSideMoreThanTwoGoalsGames(homeTeamPastMatches);
        gameQualification.Away.OneSideMoreThanTwoGoalGameAverage = OneSideMoreThanTwoGoalsGames(awayTeamPastMatches);

        // One side goal games for two to three goals
        gameQualification.Home.OneSideTwoToThreeGoalGameAverage = OneSideTwoToThreeGoalGames(homeTeamPastMatches);
        gameQualification.Away.OneSideTwoToThreeGoalGameAverage = OneSideTwoToThreeGoalGames(awayTeamPastMatches);
        
        // One side goal games less than two goals
        gameQualification.Home.OneSideLessThanTwoGoalsGameAverage = OneSideLessThanTwoGoalGames(homeTeamPastMatches);
        gameQualification.Away.OneSideLessThanTwoGoalsGameAverage = OneSideLessThanTwoGoalGames(awayTeamPastMatches);
        
        base.HandleRequest(pastGames, gameQualification, homeTeam, awayTeam);

        return gameQualification;
    }

    private static double OneSideLessThanTwoGoalGames(List<HistoricalGame> pastMatches)
    {
        return pastMatches
            .Count(i => (i.FTHG ?? 0) > 0 && (i.FTAG ?? 0) == 0 && i.FTAG + i.FTHG < 2 ||
                        (i.FTHG ?? 0) == 0 && (i.FTAG ?? 0) > 0 && i.FTAG + i.FTHG < 2)
            .Divide(pastMatches.Count);
    }

    private static double OneSideTwoToThreeGoalGames(List<HistoricalGame> pastMatches)
    {
        return pastMatches
            .Count(i => (i.FTHG ?? 0) > 0 && (i.FTAG ?? 0) == 0 && i.FTAG + i.FTHG == 3 && i.FTAG + i.FTHG == 2 ||
                        (i.FTHG ?? 0) == 0 && (i.FTAG ?? 0) > 0 && i.FTAG + i.FTHG == 3 && i.FTAG + i.FTHG == 2)
            .Divide(pastMatches.Count);
    }

    private static double OneSideMoreThanTwoGoalsGames(IReadOnlyCollection<HistoricalGame> pastMatches)
    {
        return pastMatches
            .Count(i => (i.FTHG ?? 0) > 0 && (i.FTAG ?? 0) == 0 && i.FTAG + i.FTHG >= 3 ||
                                    (i.FTHG ?? 0) == 0 && (i.FTAG ?? 0) > 0 && i.FTAG + i.FTHG >= 3)
            .Divide(pastMatches.Count);
    }
}


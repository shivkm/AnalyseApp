using AnalyseApp.Extensions;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class NoGoalGameHandler: AnalyseHandler
{
    public override GameQualification HandleRequest(List<HistoricalGame> pastGames, GameQualification gameQualification, string homeTeam, string awayTeam)
    {
        var homeTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();
        
        var awayTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();

        gameQualification.Home.LastThreeGamesWithoutGoal = GetLastThreeGames(homeTeamPastMatches)
            .All(i => i.HomeTeam == homeTeam && i.FTHG == 0 || i.AwayTeam == homeTeam && i.FTAG == 0);
        
        gameQualification.Away.LastThreeGamesWithoutGoal = GetLastThreeGames(awayTeamPastMatches)
            .All(i => i.HomeTeam == awayTeam && i.FTHG == 0 || i.AwayTeam == awayTeam && i.FTAG == 0);
        
        // No goal score by team
        gameQualification.Home.NoGoalScoredByTeamAverage = NoGoalAverage(homeTeamPastMatches, homeTeam);
        gameQualification.Away.NoGoalScoredByTeamAverage = NoGoalAverage(awayTeamPastMatches, awayTeam);

        base.HandleRequest(pastGames, gameQualification, homeTeam, awayTeam);

        return gameQualification;
    }

    private static List<HistoricalGame> GetLastThreeGames(List<HistoricalGame> pastGames) =>
        pastGames.OrderByDescending(i => DateTime.Parse(i.Date)).Take(3).ToList();

    private static double NoGoalAverage(IReadOnlyCollection<HistoricalGame> pastMatches, string team)
    {
        var home = pastMatches.Where(i => i.HomeTeam == team).Count(i => i.FTHG == 0);
        var away = pastMatches.Where(i => i.AwayTeam == team).Count(i => i.FTAG == 0);
        
        return (home + away).Divide(pastMatches.Count);
    }
}
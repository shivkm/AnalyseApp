using AnalyseApp.Extensions;
using AnalyseApp.Models;

namespace AnalyseApp.Services;


public class ZeroZeroGameHandler : AnalyseHandler
{
    public override GameQualification HandleRequest(List<HistoricalGame> pastGames, GameQualification gameQualification, string homeTeam, string awayTeam)
    {
        var homeTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();
        
        var awayTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();

        gameQualification.Home.ZeroZeroGameAverage = homeTeamPastMatches
            .Count(i => i.FTHG + i.FTAG == 0).Divide(homeTeamPastMatches.Count);
        
        gameQualification.Away.ZeroZeroGameAverage = awayTeamPastMatches
            .Count(i => i.FTHG + i.FTAG == 0).Divide(awayTeamPastMatches.Count);
        
        base.HandleRequest(pastGames, gameQualification, homeTeam, awayTeam);

        return gameQualification;
    }
}
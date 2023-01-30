using AnalyseApp.Models;

namespace AnalyseApp.Handlers;

public class NoGoalScoredHandler: AbstractAnalyseHandler
{
    public Average? Handle(double probability, NextGame lastSixGames, NextGame allGames)
    {
        var lastSix = lastSixGames.Home.NoGoalGames * LastSixGames +
                                lastSixGames.Away.NoGoalGames  * LastSixGames;
        
        var allGamesNoScore = allGames.Home.NoGoalGames * HistoricalGames +
                                 allGames.Away.NoGoalGames  * HistoricalGames;

        var average = (lastSix + allGamesNoScore) * 0.50 + 
                      allGames.HeadToHead.NoScored * HeadToHeadGames + 
                      probability * PoisonProbability;
        
        return new Average(average, average < 0.20);
    }
    
    
}
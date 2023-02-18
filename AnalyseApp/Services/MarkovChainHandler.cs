using AnalyseApp.Algorithm;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class MarkovChainHandler: AnalyseHandler
{
    public override GameQualification HandleRequest(List<HistoricalGame> pastGames, GameQualification gameQualification, string homeTeam, string awayTeam)
    {
        var homeTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();
        
        var awayTeamPastMatches = pastGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();
        
        // No goal score by team
        var homeTeamProbability = MarkovChainScoreAverageAndPoissonProbability(homeTeamPastMatches, homeTeam);
        var awayTeamProbability = MarkovChainScoreAverageAndPoissonProbability(awayTeamPastMatches, awayTeam);
        
        gameQualification.Home.MarkovChainAtLeastOneGoalProbability = homeTeamProbability.Probability;
        gameQualification.Home.MonteCarloAtLeastOneGoalProbability = MonteCarlo(homeTeamProbability.Average);
        gameQualification.Away.MarkovChainAtLeastOneGoalProbability = awayTeamProbability.Probability;
        gameQualification.Away.MonteCarloAtLeastOneGoalProbability = MonteCarlo(awayTeamProbability.Average);

        base.HandleRequest(pastGames, gameQualification, homeTeam, awayTeam);

        return gameQualification;
    }

    private static double MonteCarlo(double scoreAverage)
    {
        const int simulationIterations = 10000;
        var goalsScored  = 0;

        for (var i = 0; i < simulationIterations; i++)
        {
            var homeTeamScores = SimulateScore(scoreAverage);

            if (homeTeamScores)
            {
                goalsScored++;
            }
        }

        var bothScoreProbability = (double)goalsScored / simulationIterations;

        return bothScoreProbability;
    }
    
    private static bool SimulateScore(double accuracy)
    {
        var random = new Random();
        return random.NextDouble() < accuracy;
    }
    
    private static (double Probability, double Average) MarkovChainScoreAverageAndPoissonProbability(IList<HistoricalGame> pastGames, string team)
    {
        var homeTeamGames = pastGames
            .Where(i => i.HomeTeam == team)
            .ToList();
        
        var awayTeamGames = pastGames
            .Where(i => i.AwayTeam == team)
            .ToList();

        var markovChain = new MarkovChain();
        markovChain.AddHomeGamesBy(homeTeamGames);
        markovChain.AddAwayGamesBy(awayTeamGames);
        
        var scoreAverage = markovChain.PredictScore(team);
        var probabilities = new List<double>();

        for (var score = 0; score <= 10; score++)
        {
            var probability = GetProbabilityBy(scoreAverage, score);
            if (score > 0)
            {
                probabilities.Add(probability);
            }
        }

        var result = probabilities.Sum();

        return (result, scoreAverage);
    }
}
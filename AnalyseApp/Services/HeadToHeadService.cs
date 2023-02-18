using AnalyseApp.Algorithm;
using AnalyseApp.Extensions;
using AnalyseApp.Generics;
using AnalyseApp.Models;
using MathNet.Numerics.Distributions;

namespace AnalyseApp.Services;

public class HeadToHeadService
{
    public HeadToHead GetHeadToHeadGameAverageBy(IList<HistoricalGame> historicalGames, string homeTeam,
        string awayTeam)
    {
        // Get all head to head matches
        var pastMatches = historicalGames
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                        i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .GetGameDataBy(2016, 2023);

       // return HeadToHeadNaiveBayesBy(historicalGames, homeTeam, awayTeam);
       return null;
    }
    
    public bool HeadToHeadNaiveBayesBy(IList<HistoricalGame> pastMatches, string homeTeam, string awayTeam)
    {
        var qualified = true;
        var (homePoisonAndMarkovChainProbability, awayPoisonAndMarkovChainProbability) = PoisonAndMarkovChainProbability(pastMatches, homeTeam, awayTeam);

        // Score average and Poison probability
        var homeGoalAverage = pastMatches.GetGoalAverage(homeTeam);
        var awayGoalAverage = pastMatches.GetGoalAverage(awayTeam);
        
        var homePoisonProbability = Poisson(homeGoalAverage, 1);
        var awayPoisonProbability = Poisson(awayGoalAverage, 1);
        
        // Monte carlo probability
        var homeMonteCarloProbability = MonteCarlo(homeGoalAverage);
        var awayMonteCarloProbability = MonteCarlo(awayGoalAverage);
        
        if (homeMonteCarloProbability != 1 || awayMonteCarloProbability != 0.64)
        {
            qualified = false;
        }
        var goalGameAverage = pastMatches.GetGoalGameAverage();
        var noGoalGameAverage = pastMatches.GetNoGoalGameAverage();

        var home = homePoisonAndMarkovChainProbability * 0.35 + homePoisonProbability * 0.35 + homeMonteCarloProbability * 0.30;
        var away = awayPoisonAndMarkovChainProbability * 0.35 + awayPoisonProbability * 0.35 + awayMonteCarloProbability * 0.30;
        
        return qualified;
    }

    private static (double homeMarkovChain, double awayMarkovChain)
        PoisonAndMarkovChainProbability(IList<HistoricalGame> pastMatches, string homeTeam, string awayTeam)
    {
        var homeList = new List<double>();
        var awayList = new List<double>();
        var markovChain = new MarkovChain();
        markovChain.AddGame(pastMatches.ToList());
        var markovChainPoison = markovChain.PredictScore(homeTeam, awayTeam);
        var home = Poisson(markovChainPoison.Item1, 1);
        var away = Poisson(markovChainPoison.Item2, 1);
        
        return (home, away);
    }

    public bool TeamAnalyse(IList<HistoricalGame> pastMatches, string homeTeam, string awayTeam)
    {
        var homeTeamGames = pastMatches.Where(i => i.HomeTeam == homeTeam).ToList();
        var awayTeamGames = pastMatches.Where(i => i.AwayTeam == homeTeam).ToList();
        
        var qualified = true;
        var markovChain = new MarkovChain();
        markovChain.AddGame(homeTeamGames.ToList());
        var markovChainScoreAverage = markovChain.PredictScore(homeTeam, awayTeam);
        
        markovChain.AddGame(awayTeamGames.ToList());
        var awaymarkovChainScoreAverage = markovChain.PredictScore(homeTeam, awayTeam);
        // Markov chain and Poison probability
        var homePoisonAndMarkovChainProbability = Poisson(markovChainScoreAverage.Item1, 1);
        var awayPoisonAndMarkovChainProbability = Poisson(awaymarkovChainScoreAverage.Item2, 1);

        // Score average and Poison probability
        var homeGoalAverage = homeTeamGames.GetGoalAverage(homeTeam);
        var awayGoalAverage = awayTeamGames.GetGoalAverage(awayTeam);
        
        var homePoisonProbability = Poisson(homeGoalAverage, 1);
        var awayPoisonProbability = Poisson(awayGoalAverage, 1);
        
        // Monte carlo probability
        var homeMonteCarloProbability = MonteCarlo(homeGoalAverage);
        var awayMonteCarloProbability = MonteCarlo(awayGoalAverage);
        
        if (homeMonteCarloProbability != 1 || awayMonteCarloProbability != 0.64)
        {
            qualified = false;
        }
        var goalGameAverage = pastMatches.GetGoalGameAverage();
        var noGoalGameAverage = pastMatches.GetNoGoalGameAverage();

        var home = homePoisonAndMarkovChainProbability * 0.35 + homePoisonProbability * 0.35 + homeMonteCarloProbability * 0.30;
        var away = awayPoisonAndMarkovChainProbability * 0.35 + awayPoisonProbability * 0.35 + awayMonteCarloProbability * 0.30;
        
        return qualified;
    }
    
    private static double Poisson(double lambda, int expectedGoal)
    {
        if (double.IsNaN(lambda) || lambda == 0)
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
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

        // If this is bigger or equal to 0.50 than it is a high likelihood that both teams will score in the match.
        return bothScoreProbability;
    }
    
    private static bool SimulateScore(double accuracy)
    {
        var random = new Random();
        return random.NextDouble() * 2 < accuracy;
    }
    
}
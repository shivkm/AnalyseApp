using AnalyseApp.Extensions;
using AnalyseApp.Models;

namespace AnalyseApp.Services;


public static class CalculateAverage
{
    private const double LastEightGamesWeight = 0.30;
    private const double HistoricalGamesWeight = 0.10;
    private const double HeadToHeadGamesWeight = 0.30;
    private const double PoisonProbabilityWeight = 0.30;

    
    
    
   
    
    
    
    public static void BothScoreGames(this NextGame lastEightGames, NextGame allGames, GameProbability gameProbability, double probability)
    {
        // Average of the games where both team scored at least one Goal
        gameProbability.HomeScoredGameAverage = lastEightGames.Home.BothScoreGames * LastEightGamesWeight +
                                                allGames.Home.BothScoreGames * HistoricalGamesWeight +
                                                allGames.HeadToHead.BothTeamScored * HeadToHeadGamesWeight +
                                                probability * PoisonProbabilityWeight;

        gameProbability.AwayScoredGameAverage  = lastEightGames.Away.BothScoreGames * LastEightGamesWeight +
                                                 allGames.Away.BothScoreGames * HistoricalGamesWeight +
                                                 allGames.HeadToHead.BothTeamScored * HeadToHeadGamesWeight +
                                                 probability * PoisonProbabilityWeight;

        // Average of the teams score and conceded scores
        gameProbability.HomeScoreAverage = lastEightGames.Home.ScoredGoal * 0.65 + allGames.Home.ScoredGoal * 0.35;
        gameProbability.HomeConcededAverage = lastEightGames.Home.AllowGoals * 0.65 + allGames.Home.AllowGoals * 0.35;

        gameProbability.AwayScoreAverage = lastEightGames.Away.ScoredGoal * 0.65 + allGames.Away.ScoredGoal * 0.35;
        gameProbability.AwayConcededAverage = lastEightGames.Away.AllowGoals * 0.65 + allGames.Away.AllowGoals * 0.35;

        var monteCarloApproveBothScored = MonteCarloApproveBothScored(
            gameProbability.HomeScoreAverage,
            gameProbability.AwayScoreAverage
        );

        var homeMarkovChain = MarkovChainApproveBothScored(gameProbability.HomeScoreAverage);
        var awayMarkovChain = MarkovChainApproveBothScored(gameProbability.AwayScoreAverage);
        
        var finalAverage = gameProbability.HomeScoredGameAverage * 0.50 +
                                    gameProbability.AwayScoredGameAverage * 0.50;
        
        var limit = GetPassingLimitBy(gameProbability.League);

        // maybe for both score we dont need rthat 
        //if (lastEightGames.Away.LastSixGamesBothScored || lastEightGames.Home.LastSixGamesBothScored)
        //    return;

        if (gameProbability.HomeScoredGameAverage > limit && gameProbability.AwayScoredGameAverage > limit &&
            gameProbability.HalftimeGoalAverage >= limit && !gameProbability.Qualified)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = finalAverage;
            gameProbability.ProbabilityKey = nameof(BothScoreGames);
        }
    }

    public static void Over15GoalsGames(this NextGame lastEightGames, NextGame allGames, GameProbability gameProbability, double probability)
    {
        var home = gameProbability.HomeScoredGameAverage * 0.60 + probability * 0.60;
        var away = gameProbability.AwayScoredGameAverage * 0.60 + probability * 0.60;
        
        var limit = GetPassingLimitBy(gameProbability.League);
        
        if (lastEightGames.Away.LastFiveGamesOver && lastEightGames.Home.LastFiveGamesOver ||
            allGames.HeadToHead.MoreThanTwoScored > 0.90 || allGames.HeadToHead.GamesPlayed < 3)
            return;
        
        if (home > limit && away > limit)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = home * 0.5 + away * 0.5;
            gameProbability.ProbabilityKey = nameof(Over15GoalsGames);
        }
    }
    
    public static void MoreThanTwoGoalsGames(this NextGame lastEightGames, NextGame allGames, GameProbability gameProbability, double probability)
    {
        gameProbability.HomeOverTwoGoalGameAverage = lastEightGames.Home.MoreThanTwoGoalsGames * LastEightGamesWeight  +
                                                     allGames.Home.MoreThanTwoGoalsGames * HistoricalGamesWeight +
                                                     allGames.HeadToHead.MoreThanTwoScored * HeadToHeadGamesWeight +
                                                     probability * PoisonProbabilityWeight;
        
        gameProbability.AwayOverTwoGoalGameAverage =  lastEightGames.Away.MoreThanTwoGoalsGames * LastEightGamesWeight  +
                                                      allGames.Away.MoreThanTwoGoalsGames * HistoricalGamesWeight +
                                                      allGames.HeadToHead.MoreThanTwoScored * HeadToHeadGamesWeight +
                                                      probability * PoisonProbabilityWeight;


        var limit = GetPassingLimitBy(gameProbability.League, 1);
         var finalAverage = gameProbability.HomeOverTwoGoalGameAverage * 0.50 +
                                  gameProbability.AwayOverTwoGoalGameAverage * 0.50;
       
        if (lastEightGames.Away.LastFiveGamesOver || lastEightGames.Home.LastFiveGamesOver || 
            lastEightGames.Home.MoreThanTwoGoalsGames > 0.90 || lastEightGames.Away.MoreThanTwoGoalsGames > 0.90 ||
            allGames.HeadToHead.MoreThanTwoScored > 0.90 || allGames.HeadToHead.GamesPlayed < 3)
            return;

        if (lastEightGames.Home is { LastTwoGamesWithZeroGoal: true, LastTwoGamesLessThanTwoGoals: true })
            gameProbability.PossibleMoreThanTwoGoals = true;
        
        if (gameProbability.HalftimeGoalAverage > limit && !gameProbability.Qualified)
        {
            if (gameProbability.HomeOverTwoGoalGameAverage > limit && gameProbability.AwayOverTwoGoalGameAverage > limit)
            {
                gameProbability.Qualified = true;
                gameProbability.Probability = finalAverage;
                gameProbability.ProbabilityKey = nameof(MoreThanTwoGoalsGames);
            }
            
            if (gameProbability.HomeOverTwoGoalGameAverage is > 0.55 and < 0.88 && gameProbability.AwayOverTwoGoalGameAverage > limit ||
                gameProbability.AwayOverTwoGoalGameAverage is > 0.55 and < 0.88 && gameProbability.HomeOverTwoGoalGameAverage > limit)
            {
                gameProbability.Qualified = true;
                gameProbability.Probability = finalAverage;
                gameProbability.ProbabilityKey = nameof(MoreThanTwoGoalsGames);
            }
        }
    }
    
    public static void TwoToThreeGoalsGames(this NextGame lastEightGames, NextGame allGames, GameProbability gameProbability, double probability)
    {
        var homeAverage = lastEightGames.Home.TwoToThreeGoalGames * LastEightGamesWeight  +
                              allGames.Home.TwoToThreeGoalGames * HistoricalGamesWeight +
                              allGames.HeadToHead.TwoToThreeScored * HeadToHeadGamesWeight +
                              probability * PoisonProbabilityWeight;
        
        var awayAverage = lastEightGames.Away.TwoToThreeGoalGames * LastEightGamesWeight  +
                                  allGames.Away.TwoToThreeGoalGames * HistoricalGamesWeight +
                                  allGames.HeadToHead.TwoToThreeScored * HeadToHeadGamesWeight +
                                  probability * PoisonProbabilityWeight;
        
        if (lastEightGames.Away.LastFiveGamesLess || lastEightGames.Home.LastFiveGamesLess ||
            allGames.HeadToHead.GamesPlayed < 3)
            return;
        
        var limit = GetPassingLimitBy(gameProbability.League, 2);
        var finalAverage = homeAverage * 0.50 + awayAverage * 0.50;
        // Home and Away both are able at least 68% to score more than two goals and the previous probability is not bigger than current
        // than this would be qualified.
        if (homeAverage > limit && awayAverage > limit && !gameProbability.Qualified)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = finalAverage;
            gameProbability.ProbabilityKey = nameof(TwoToThreeGoalsGames);
        }
    }

    public static void LessThanThreeGoalsGames(this NextGame lastEightGames, NextGame allGames, GameProbability gameProbability, double probability)
    {
        var homeAverage = lastEightGames.Home.LessThanThreeGoalsAccuracy * LastEightGamesWeight  +
                          allGames.Home.LessThanThreeGoalsAccuracy * HistoricalGamesWeight +
                          allGames.HeadToHead.LessThanThreeGoal * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;
        
        var awayAverage = lastEightGames.Away.LessThanThreeGoalsAccuracy * LastEightGamesWeight  +
                          allGames.Away.LessThanThreeGoalsAccuracy * HistoricalGamesWeight +
                          allGames.HeadToHead.LessThanThreeGoal * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;
        
        if (lastEightGames.Away.LastFiveGamesLess || lastEightGames.Home.LastFiveGamesLess)
            return;
        
        var limit = GetPassingLimitBy(gameProbability.League, 2);
        var finalAverage = homeAverage * 0.50 + awayAverage * 0.50;
        
        if ((homeAverage > limit || homeAverage > 0.53 && awayAverage > limit + 0.1) &&
            (awayAverage > limit || awayAverage > 0.53 && homeAverage > limit + 0.1) &&
            !gameProbability.Qualified &&
            allGames.HeadToHead.GamesPlayed > 3)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = finalAverage;
            gameProbability.ProbabilityKey = nameof(LessThanThreeGoalsGames);
        }
    }
    
    
    public static void WinGames(this NextGame lastSixGames, NextGame allGames, GameProbability gameProbability, double homeWinProbability, double awayProbability)
    {
        var homeWinAverage = lastSixGames.Home.WinAccuracy * LastEightGamesWeight +
                                     allGames.Home.WinAccuracy * HistoricalGamesWeight +
                                     allGames.HeadToHead.HomeWin * HeadToHeadGamesWeight +
                                     homeWinProbability * PoisonProbabilityWeight;

        var awayWinAverage = lastSixGames.Away.WinAccuracy * LastEightGamesWeight +
                                     allGames.Away.WinAccuracy * HistoricalGamesWeight +
                                     allGames.HeadToHead.AwayWin * (HeadToHeadGamesWeight + 0.10) +
                                     awayProbability * PoisonProbabilityWeight;

        var limit = GetPassingLimitBy(gameProbability.League, 3);

        if (allGames.HeadToHead.GamesPlayed < 2)
            return;

        if (homeWinAverage > awayWinAverage && homeWinAverage > limit && !gameProbability.Qualified)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = homeWinAverage;
            gameProbability.ProbabilityKey = "HomeWin";
        }
        if (homeWinAverage < awayWinAverage && awayWinAverage > limit && gameProbability.HomeConcededAverage > limit)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = awayWinAverage;
            gameProbability.ProbabilityKey = "AwayWin";
        }
    }

    
    private static bool MonteCarloApproveBothScored(double homeTeamScoreAverage, double awayTeamScoreAverage)
    {
        const int simulationIterations = 10000;
        var bothScoreCount = 0;

        for (var i = 0; i < simulationIterations; i++)
        {
            var homeTeamScores = SimulateScore(homeTeamScoreAverage);
            var awayTeamScores = SimulateScore(awayTeamScoreAverage);

            if (homeTeamScores && awayTeamScores)
            {
                bothScoreCount++;
            }
        }

        var bothScoreProbability = (double)bothScoreCount / simulationIterations;

        // If this is bigger or equal to 0.50 than it is a high likelihood that both teams will score in the match.
        return bothScoreProbability >= 0.5;
    }


    private static (double Over, double Unter) MonteCarloOverLessProbability(double goalsThreshold)
    {
        const int numSimulations = 10000;
        var overCount = 0;
        var underCount = 0;
        var random = new Random();

        for (var i = 0; i < numSimulations; i++)
        {
            var goals = random.NextGaussian(goalsThreshold, 0.5);
            if (goals > goalsThreshold)
            {
                overCount++;
            }
            else
            {
                underCount++;
            }
        }

        var overProb = (double)overCount / numSimulations;
        var underProb = (double)underCount / numSimulations;

        return (overProb, underProb);
    }
    
    private static (double Over, double Unter) MarkovChainOverLessProbability(double goalsThreshold)
    {
        const int numSimulations = 10000;
        // Define the states for the Markov Chain
        const int UNDER = 0;
        const int OVER = 1;

        // Define the transition matrix for the Markov Chain
        double[,] transitionMatrix = 
        {
            {0.6, 0.4},
            {0.5, 0.5}
        };
        
        // Define the initial probabilities
        double[] initialProbabilities = 
        {
            goalsThreshold,
            1.0 - goalsThreshold
        };
        // Run the Markov Chain simulation for 10 steps
        const int steps = 10;
        var currentProbabilities = initialProbabilities;
        for (var i = 0; i < steps; i++)
        {
            var nextProbabilities = new double[2];
            for (var j = 0; j < 2; j++)
            {
                nextProbabilities[j] = 0;
                for (var k = 0; k < 2; k++)
                {
                    nextProbabilities[j] += currentProbabilities[k] * transitionMatrix[k, j];
                }
            }
            currentProbabilities = nextProbabilities;
        }
        return (currentProbabilities[OVER], currentProbabilities[UNDER]);
    }


    private static bool MarkovChainApproveBothScored(double teamScoreAverage)
    {
        // Transitions between game states
        var transitionMatrix  = new Dictionary<double, double>
        {
            { 0.0, teamScoreAverage },
            { 1.0, 1.0 - teamScoreAverage }
        };
        
        // Initial game state (0 = Score, 1 = don't score)
        var scoreState = 0.0;

        // Define the number of steps to take
        const int steps = 10;

        // Random number generator
        var random = new Random();
        
        // Simulate the Markov Chain
        for (var i = 0; i < steps; i++)
        {
            // Pick a random number between 0 and 1
            var randomNumber = random.NextDouble();

            // Determine the next state based on the transition matrix
            var cumulativeProbability = 0.0;
            foreach (var transition in transitionMatrix)
            {
                cumulativeProbability += transition.Value;
                if (randomNumber > cumulativeProbability) 
                    continue;
                
                scoreState = transition.Key;
                break;
            }
        }

        // Check if the final state is "Score"
        return scoreState == 0.0;
    }
    
    private static bool SimulateScore(double accuracy)
    {
        var random = new Random();
        return random.NextDouble() < accuracy;
    }

    private static double GetPassingLimitBy(string league, int betType = 0)
    {
        const double defaultLimit = 0.68;
        var championship = defaultLimit;
        var serieA = defaultLimit;
        var premierLeagueBundesliga = defaultLimit;
        var frenchLeague = defaultLimit;
        
        if (betType == 0)
        {
            championship = 0.60;
            premierLeagueBundesliga = 0.62;
            frenchLeague = 0.62;
        }
        if (betType == 1)
        {
            championship = 0.60;
            premierLeagueBundesliga = 0.64;
            frenchLeague = 0.64;
            serieA = 0.64;
        }
        if (betType == 2)
        {
            championship = 0.58;
            premierLeagueBundesliga = 0.58;
            frenchLeague = 0.58;
            serieA = 0.58;
        }
        if (betType == 3)
        {
            championship = 0.70;
            premierLeagueBundesliga = 0.70;
            frenchLeague = 0.70;
            serieA = 0.70;
        }
        switch (league)
        {
            // Championship and Serie A
            case "E1":
                return championship;
            case "I1":
                return serieA;
            // Premier und Bundesliga
            case "E0":
            case "D1":
            case "D2":
                return premierLeagueBundesliga;
            case "F1":
                return frenchLeague;
            default:
                return defaultLimit;
        }
    }
}
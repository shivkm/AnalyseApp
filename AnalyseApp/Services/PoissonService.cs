using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class PoissonService: IPoissonService
{
    private const double FiftyPercentage = 0.50;

    public double GetProbabilityBy(string teamName, bool atHome, bool currentForm, List<Matches> historicalMatches)
    {
        var teamAverages = historicalMatches.GetTeamAverageBy(teamName);

        var playingSideAvg = atHome ? teamAverages.ScoreAvgAtHome : teamAverages.ScoreAvgAtAway;
        
        var allGamesGoalProbability = teamAverages.ScoreAvg.GetScoredGoalProbabilityBy();
        var playingSideGoalProbability = playingSideAvg.GetScoredGoalProbabilityBy();

        double finalProbability;
        var probability = allGamesGoalProbability * FiftyPercentage + playingSideGoalProbability * FiftyPercentage;
         
        if (currentForm)
        {
            var weightedHomeValues = historicalMatches.CalculateWeightedAverage(teamName);
            var weightedHomeAvg = weightedHomeValues.ScoredAvg / weightedHomeValues.ConcededAvg;
            var weightedGoalProbability = weightedHomeAvg.GetScoredGoalProbabilityBy();

            finalProbability = probability * FiftyPercentage + weightedGoalProbability * FiftyPercentage;

            return finalProbability;
        }
        
        var emaGoalProbability = teamAverages.ExponentialMovingAvg.GetScoredGoalProbabilityBy();
        finalProbability = probability * FiftyPercentage + emaGoalProbability * FiftyPercentage;

        return finalProbability;
    }
}
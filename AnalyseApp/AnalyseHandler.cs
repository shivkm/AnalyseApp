using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.Models;
using MathNet.Numerics.Distributions;

namespace AnalyseApp;

public record TeamQualification
{
    public int GamePlayed { get; set; }
    public bool LastThreeGamesWithoutGoal { get; set; }
    public double NoGoalScoredByTeamAverage { get; set; }
    public double HalfTimeProbability { get; set; }
    public double HalftimeScoreAverage { get; set; }
    public double ZeroZeroGameAverage { get; set; }
    public double OneSideMoreThanTwoGoalGameAverage { get; set; }
    public double OneSideTwoToThreeGoalGameAverage { get; set; }
    public double OneSideLessThanTwoGoalsGameAverage { get; set; }
    public double MarkovChainAtLeastOneGoalProbability { get; set; }
    public double MonteCarloAtLeastOneGoalProbability { get; set; }
}

public record GameQualification
{
    public TeamQualification Home { get; set; } = default!;
    public TeamQualification Away { get; set; } = default!;
    public double PoissonBothScoreProbability { get; set; }
    public double PoissonMoreThanTwoGoalsProbability { get; set; }
    public double PoissonLessThanThreeGoalsProbability { get; set; }
    public double PoissonTwoToThreeGoalsProbability { get; set; }
}


public abstract class AnalyseHandler : IAnalyseHandler
{
    
    protected const string ZeroGoal = "ZeroGoal";
    protected const string LessThanTwoScore = "LessThanTwoScore";
    protected const string TwoToThreeScored = "TwoToThreeScored";
    protected const string BothTeamScoreGoal = "BothTeamScoreGoal";
    protected const string MoreThanTwoGoals = "MoreThanTwoGoals";

    public IAnalyseHandler SetNext(IAnalyseHandler handler) => handler;

    public virtual GameQualification HandleRequest(List<HistoricalGame> pastGames,
        GameQualification gameQualification,
        string homeTeam, string awayTeam)
    {
        return gameQualification;
    }

    protected static double GetProbabilityBy(double average, int expectedGoal)
    {
        if (double.IsNaN(average) || average == 0)
            return 0;

        var poisson = new Poisson(average);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }
}
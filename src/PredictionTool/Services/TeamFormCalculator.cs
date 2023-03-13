using MathNet.Numerics.Distributions;
using PredictionTool.Extensions;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class TeamFormCalculator
{
    private readonly List<Game> _games;

    public TeamFormCalculator(List<Game> games)
    {
        _games = games;
    }

    /// <summary>
    /// Calculate team performance
    /// </summary>
    /// <param name="team"></param>
    /// <param name="numberOfGames"></param>
    /// <returns></returns>
    public TeamForm CalculateForm(string team, int numberOfGames)
    {
        var recentGames = _games
            .Where(g => g.Home == team || g.Away == team)
            .OrderByDescending(g => g.DateTime)
            .Take(numberOfGames)
            .ToList();

        var winRate = recentGames
            .Count(i => i.FullTimeResult == "H" && i.Home == team || i.FullTimeResult == "A" && i.Away == team)
            .Divide(recentGames.Count);
        
        var lossRate = recentGames
            .Count(i => i.FullTimeResult == "A" && i.Home == team || i.FullTimeResult == "H" && i.Away == team)
            .Divide(recentGames.Count);
        
        var drawRate = recentGames
            .Count(i => i.FullTimeResult == "D")
            .Divide(recentGames.Count);
        
        var goalsFor = recentGames.CalculateScoredGoalAccuracy(team);
        var goalsAgainst = recentGames.CalculateConcededGoalAccuracy(team);
        var halftimeGoalFor = recentGames.CalculateHalftimeScoreGoalAccuracy(team);
        var halftimeGoalsAgainst = recentGames.CalculateHalftimeConcededGoalAccuracy(team);
        var homeGoalFor = recentGames
            .Count(i => i.FullTimeHomeScore > 0 && i.Home == team)
            .Divide(recentGames.Count(i => i.Home == team));
        
        var homeGoalAgainst = recentGames
            .Count(i => i.FullTimeAwayScore > 0 && i.Home == team)
            .Divide(recentGames.Count(i => i.Home == team));
        
        var awayGoalFor = recentGames
            .Count(i => i.FullTimeAwayScore > 0 && i.Away == team)
            .Divide(recentGames.Count(i => i.Away == team));
        
        var awayGoalAgainst = recentGames
            .Count(i => i.FullTimeHomeScore > 0 && i.Away == team)
            .Divide(recentGames.Count(i => i.Home == team));
        
        var noGoal = recentGames
            .Count(i => i.FullTimeAwayScore == 0 && i.Away == team || i.FullTimeHomeScore == 0 && i.Home == team)
            .Divide(recentGames.Count);
        
        var oneSideGoal = recentGames
            .Count(i => i is { FullTimeHomeScore: > 0, FullTimeAwayScore: 0 } or 
                                { FullTimeHomeScore: 0, FullTimeAwayScore: > 0 })
            .Divide(recentGames.Count);
        
        var score = CalculateScoreProbabilityBy(goalsFor);
        var conceded = CalculateScoreProbabilityBy(goalsAgainst);

        return new TeamForm
        {
            Team = team,
            NumberOfGames = recentGames.Count,
            WinRate = winRate,
            DrawRate = drawRate,
            LossRate = lossRate,
            GoalsForPerGame = goalsFor,
            GoalsAgainstPerGame = goalsAgainst,
            HalftimeGoalsPerGame = halftimeGoalFor,
            HalftimeGoalsAgainstPerGame = halftimeGoalsAgainst,
            GoalProbability = score,
            ConcededProbability = conceded,
            NoGoalPerformance = 2 * noGoal / 2.0,
            OneSidedGoalPerformance = 2 * oneSideGoal / 2.0,
            HomeSideGoalPerformance = CalculateGoalPerformance(homeGoalFor, homeGoalAgainst),
            AwaySideGoalPerformance = CalculateGoalPerformance(awayGoalFor, awayGoalAgainst),
            GoalPerformance = CalculateGoalPerformance(homeGoalFor, homeGoalAgainst)
        };
    }

    /// <summary>
    /// The method calculate overall goal-scoring ability relative to their opponents,
    /// with a higher value indicating a stronger attacking team and a lower value indicating a stronger defensive team.
    /// </summary>
    /// <param name="goalsForPerGame">Team scored goal average</param>
    /// <param name="goalsAgainstPerGame">Team conceded goal average</param>
    /// <returns></returns>
    private static double CalculateGoalPerformance(double goalsForPerGame, double goalsAgainstPerGame)
     => (2 * goalsForPerGame + goalsAgainstPerGame) / 3.0;
    
    private static double CalculateScoreProbabilityBy(double average)
    {
        var scoresProb = new List<double>();
        for (var i = 1; i <= 10; i++)
        {
            var prob = CalculatePoissonProbability(average, i);
            scoresProb.Add(prob);
        }

        return scoresProb.Sum();
    }
    
    private static double CalculatePoissonProbability(double lambda, int expectedGoal)
    {
        if (double.IsNaN(lambda) || lambda == 0)
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }
}

public record TeamForm
{
    public string Team { get; set; }
    public int NumberOfGames { get; set; }
    public double WinRate { get; set; }
    public double DrawRate { get; set; }
    public double LossRate { get; set; }
    public double GoalsForPerGame { get; set; }
    public double GoalsAgainstPerGame { get; set; }
    public double HalftimeGoalsPerGame { get; set; }
    public double HalftimeGoalsAgainstPerGame { get; set; }
    public double HomeSideGoalPerformance { get; set; }
    public double AwaySideGoalPerformance { get; set; }
    public double GoalProbability { get; set; }
    public double ConcededProbability { get; set; }
    public double GoalPerformance { get; set; }
    public double NoGoalPerformance { get; set; }
    public double OneSidedGoalPerformance { get; set; }
}

using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record TeamGoalAverage(
    GoalPower Overall,
    GoalPower Recent
);

public record GoalPower(
    double ScoredGoalProbability, 
    double ConcededGoalProbability
);

public class TeamPerformance
{
    public string TeamName { get; set; }
    private float totalGoalsScored = 0;
    private float totalGoalsConceded = 0;
    private int matchesCount = 0;

    public float AvgGoalsScored => matchesCount > 0 ? totalGoalsScored / matchesCount : 0;
    public float AvgGoalsConceded => matchesCount > 0 ? totalGoalsConceded / matchesCount : 0;

    public void UpdatePerformance(int? goalsScored, int? goalsConceded)
    {
        if (goalsScored.HasValue && goalsConceded.HasValue)
        {
            totalGoalsScored += goalsScored.Value;
            totalGoalsConceded += goalsConceded.Value;
            matchesCount++;
        }
    }
}

public class MatchOutcome
{
    [ColumnName("Label")]
    public bool OverTwoGoals { get; set; }
}

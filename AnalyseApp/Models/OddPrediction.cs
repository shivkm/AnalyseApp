using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record OddPrediction
{
    [ColumnName("PredictedLabel")]
    public string Outcome { get; set; }
}

public record OverUnderPrediction
{
    [ColumnName("PredictedLabel")]
    public string OverUnderGoals { get; set; }
}

public record GoalGoalsPrediction
{
    [ColumnName("PredictedLabel")]
    public string GoalGoals { get; set; }
}

public record TwoToThreeGoalsPrediction
{
    [ColumnName("PredictedLabel")]
    public string TwoToThreeGoals { get; set; }
}
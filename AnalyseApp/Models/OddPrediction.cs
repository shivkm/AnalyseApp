using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record MLPrediction
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }
}

public record OverUnderPrediction
{
    [ColumnName("PredictedLabel")]
    public bool OverUnderGoals { get; set; }
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
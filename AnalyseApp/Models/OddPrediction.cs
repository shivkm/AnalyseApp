using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record MatchOutcomePrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }
    public float Score { get; set; }
}

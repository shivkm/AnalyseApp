using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record MatchPrediction
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }
    
    public string Msg { get; init; } = default!;
    public double Percentage { get; set; } = default!;
    public bool Qualified { get; init; }
}
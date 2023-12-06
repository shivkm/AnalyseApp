using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record OddPrediction
{
    [ColumnName("PredictedLabel")]
    public string Outcome { get; set; }
    
}
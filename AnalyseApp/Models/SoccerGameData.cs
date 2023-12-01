using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record SoccerGameData(
    string HomeTeam, 
    string AwayTeam, 
    float HomeScored, 
    float AwayScored, 
    float HomeHalfScored, 
    float AwayHalfScored, 
    bool IsOverTwoGoals
);

public class SoccerGamePrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsOverTwoGoals { get; set; }
}
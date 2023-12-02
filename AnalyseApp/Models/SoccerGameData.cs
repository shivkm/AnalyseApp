using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record SoccerGameData(
    string HomeTeam, 
    string AwayTeam, 
    float HomeScored, 
    float AwayScored, 
    float HomeHalfScored, 
    float AwayHalfScored, 
    bool IsOverTwoGoals,
    bool BothTeamGoals,
    bool HomeTeamWin,
    bool AwayTeamWin
);

public class SoccerGamePredictionOverTwoGoals
{
    [ColumnName("PredictedLabel")]
    public bool IsOverTwoGoals { get; set; }
    
    [ColumnName("Probability")]
    public float ProbabilityOverTwoGoals { get; set; }
}

public class SoccerGamePredictionBothTeamsScore
{
    [ColumnName("PredictedLabel")]
    public bool BothTeamGoals { get; set; }
    
    [ColumnName("Probability")]
    public float ProbabilityBothTeamsScore { get; set; }
}

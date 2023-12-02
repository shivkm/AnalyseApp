namespace AnalyseApp.Models;

public record SoccerGameData(
    float HomeTeam, 
    float AwayTeam, 
    float HomeScored, 
    float AwayScored, 
    float HomeHalfScored, 
    float AwayHalfScored, 
    bool IsOverTwoGoals, 
    bool BothTeamGoals, 
    bool TwoToThreeGoals, 
    bool HomeWin, 
    bool AwayWin
);
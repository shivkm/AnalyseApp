namespace AnalyseApp.Models;
public record MatchData
{
    public string Home { get; set; }
    public string Away { get; set; }

    public TeamData HomeTeam { get; set; }
    public TeamData AwayTeam { get; set; }
    public HeadToHeadData HeadToHeadData { get; set; }
    
    public DateTime Date { get; set; }
    public int HomeScoredGoals { get; set; }
    public int AwayScoredGoals { get; set; }
    public int HomeConcededGoals { get; set; }
    public int AwayConcededGoals { get; set; }
    public int TotalLeagueHomeGoals { get; set; }
    public int TotalLeagueAwayGoals { get; set; }  
    public float HomeScoredGoalsAverage { get; set; }
    public float AwayScoredGoalsAverage { get; set; }
    public float HomeConcededGoalsAverage { get; set; }
    public float AwayConcededGoalsAverage { get; set; }
    public float HomeHalfTimeScoredGoalsAverage { get; set; }
    public float AwayHalfTimeScoredGoalsAverage { get; set; }
    public float HomeHalfTimeConcededGoalsAverage { get; set; }
    public float AwayHalfTimeConcededGoalsAverage { get; set; }
    public double HomeZeroZeroMatchAverage { get; set; }
    public double AwayZeroZeroMatchAverage { get; set; }
    public double HomeUnderThreeGoalsMatchAverage { get; set; }
    public double AwayUnderThreeGoalsMatchAverage { get; set; }
    public double HomeOverTwoGoalsMatchAverage { get; set; }
    public double AwayTwoToThreeGoalsMatchAverage { get; set; }
    public double HomeTwoToThreeGoalsMatchAverage { get; set; }
    public double AwayOverTwoGoalsMatchAverage { get; set; }
    public double HomeGoalGoalMatchAverage { get; set; }
    public double AwayGoalGoalMatchAverage { get; set; }

    public bool OverUnderTwoGoals { get; set; } 
    public bool BothTeamsScored { get; set; }
    public bool TwoToThreeGoals { get; set; }
    public bool HomeWin { get; set; }
    public bool AwayWin { get; set; }
}

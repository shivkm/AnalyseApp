namespace AnalyseApp.Models;

public record MatchData
{
    public string Home { get; set; }
    public string Away { get; set; }
    public DateTime Date { get; set; }
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
    public double AwayOverTwoGoalsMatchAverage { get; set; }
    public double HomeGoalGoalMatchAverage { get; set; }
    public double AwayGoalGoalMatchAverage { get; set; }

    public bool OverUnderTwoGoals { get; set; } 
    public bool BothTeamsScored { get; set; }
    public bool TwoToThreeGoals { get; set; }
    public bool HomeWin { get; set; }
    public bool AwayWin { get; set; }
}

public record TeamData
{ 
    public string TeamName { get; set; }
    public float ConcededGoalsAverage { get; set; }
    public float ScoredGoalsAverage { get; set; }
    public float HalfTimeScoredGoalAverage { get; set; }
    public float HalfTimeConcededGoalAverage { get; set; }
    
    public double ZeroZeroMatchAverage { get; set; }
    public double UnderThreeGoalsMatchAverage { get; set; }
    public double OverTwoGoalsMatchAverage { get; set; }
    public double GoalGoalsMatchAverage { get; set; }
    public float TwoToThreeMatchAverage { get; set; }
    public float ScoredShotsAverage { get; set; }
    public float ScoredTargetShotsAverage { get; set; }
    public float ConcededShotsAverage { get; set; }
    public float ConcededTargetShotsAverage { get; set; }
};
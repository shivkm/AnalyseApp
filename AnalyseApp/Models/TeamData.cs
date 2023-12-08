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
    public float HomeScoredShotsAverage { get; set; }
    public float AwayScoredShotsAverage { get; set; }
    public float HomeConcededShotsAverage { get; set; }
    public float AwayConcededShotsAverage { get; set; }
    public float HomeScoredTargetShotsAverage { get; set; }
    public float AwayScoredTargetShotsAverage { get; set; }
    public float HomeConcededTargetShotsAverage { get; set; }
    public float AwayConcededTargetShotsAverage { get; set; }

    public bool OverUnderTwoGoals { get; set; } 
    public bool BothTeamsScored { get; set; }
    public bool TwoToThreeGoals { get; set; }
}

public record TeamData
{ 
    public string TeamName { get; set; }
    public float ConcededGoalsAverage { get; set; }
    public float ScoredGoalsAverage { get; set; }
    public float HalfTimeScoredGoalAverage { get; set; }
    public float HalfTimeConcededGoalAverage { get; set; }
    public float ScoredShotsAverage { get; set; }
    public float ScoredTargetShotsAverage { get; set; }
    public float ConcededShotsAverage { get; set; }
    public float ConcededTargetShotsAverage { get; set; }
};
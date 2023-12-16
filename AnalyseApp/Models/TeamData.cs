namespace AnalyseApp.Models;

public record TeamData
{ 
    public string TeamName { get; set; }
    public int MatchCount { get; set; }
    public int FieldMatchCount { get; set; }
    public int ScoredGoalMatchCount { get; set; }
    public int FieldScoredGoalMatchCount { get; set; }
    public string Output { get; set; }
    
    public bool LastMatchNotScored { get; set; }
    public bool LastTwoScored { get; set; }
    public bool LastAndThirdLastScored { get; set; }
    
    
    
    public bool ScoredGoalInLastThreeMatches { get; set; }
    
    
    
    
    public int ScoredGoals { get; set; }
    public int ConcededGoals { get; set; }
    public int TotalLeagueHomeGoals { get; set; }
    public int TotalLeagueAwayGoals { get; set; }  
    public float ConcededGoalsAverage { get; set; }
    public float ScoredGoalsAverage { get; set; }
    public float HalfTimeScoredGoalAverage { get; set; }
    public float HalfTimeConcededGoalAverage { get; set; }
    
    public double ZeroZeroMatchAverage { get; set; }
    public double UnderThreeGoalsMatchAverage { get; set; }
    public double OverTwoGoalsMatchAverage { get; set; }
    public double GoalGoalsMatchAverage { get; set; }
    public double TwoToThreeMatchAverage { get; set; }
    public float ScoredShotsAverage { get; set; }
    public float ScoredTargetShotsAverage { get; set; }
    public float ConcededShotsAverage { get; set; }
    public float ConcededTargetShotsAverage { get; set; }
};
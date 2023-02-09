using Microsoft.ML.Data;

namespace AnalyseApp.Models;


public record TeamPerformance
{
    public int MatchesPlayed { get; set; }
    public int BothScoredMatchPlayed { get; set; }
    public int MoreThanTwoGoalMatchPlayed { get; set; }
    public double CompositeWin { get; set; }
    public double CompositeMoreThanTwoGoals { get; set; }
    public double CompositeScoreGoals { get; set; }
    public double CompositeDefense { get; set; }
    public double CompositeOffsideAndFouls { get; set; }
    public double CompositeLessGoals { get; set; }
    public double NoGoalMatches { get; set; }
    public double Offsides { get; set; }
    public double WinOneSideGoalMatches { get; set; }
    public double OneSideGoalMatches { get; set; }
    public double CompositeHalftimeGoals { get; set; }
}









public record Result
{
    public double? Home { get; set; }
    public double? Away { get; set; }
}

public record Average(double Value, bool Qualified, string Msg = default);



public record GameProbability
{
    public string? Title { get; set; }
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string League { get; set; }
    public string Msg { get; set; }
    public string? ProbabilityKey { get; set; }
    public double Probability { get; set; }
    public bool Qualified { get; set; }
    public double NoGoalAverage { get; set; }
    public double HomeScoreAverage { get; set; }
    public double HomeConcededAverage { get; set; }
    public double AwayScoreAverage { get; set; }
    public double AwayConcededAverage { get; set; }
    public double HomeScoredGameAverage { get; set; }
    public double AwayScoredGameAverage { get; set; }
    public double HomeOverTwoGoalGameAverage { get; set; }
    public double AwayOverTwoGoalGameAverage { get; set; }
    public double HalftimeGoalAverage { get; set; }
    public bool PossibleMoreThanTwoGoals { get; set; }
}

public class MatchData
{
    public string HomeTeam { get; set; }
    public string AwayTeam  { get; set; }
    public float HomeTeamGoals { get; set; }
    public float AwayTeamGoals { get; set; }
    public float HomeTeamShots { get; set; }
    public float AwayTeamShots { get; set; }
    public float HomeTeamHalfTimeGoals { get; set; }
    public float AwayTeamHalfTimeGoals { get; set; }
    public float HomeTeamFullTimeGoals { get; set; }
    public float AwayTeamFullTimeGoals { get; set; }
    public float PredictedLabel { get; set; }
}

public class MatchPrediction
{
    [ColumnName("Score")]
    public float PredictedLabel { get; set; }
}
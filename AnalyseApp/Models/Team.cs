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



public record Game
{
    public string? Title { get; set; }
    public GameAverage SelfAnalysis { get; set; }
    public PoissonProbability PoissonProbability { get; set; }
}

public record GameAverage
{
    public Average? CurrentSeason { get; set; }
    public Average? LastSixMatches { get; set; }
    public Average? HistoricalMatches { get; set; }
    public PoissonProbability PoissonProbability { get; set; }
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
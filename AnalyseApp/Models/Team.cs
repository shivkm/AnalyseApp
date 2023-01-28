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

public record Average
{
    public Result ZeroZeroGame { get; set; } = new();
    public Result OneSideResult { get; set; } = new();
    public Result ScoredGames { get; set; }= new();
    public Result TwoScoredGame { get; set; }= new();
    public Result TwoScoredGameCount { get; set; }= new();
    public Result HalftimeScoredGames { get; set; } = new();
    public Result MoreThanTwoScoredAverage { get; set; } = new();
    public Result BothScoredAverage { get; set; } = new();
    public HeadToHead HeadToHeadGame { get; set; } = new();
}

public record HeadToHead
{
    public int TotalGames { get; set; }
    public double BothTeamScore { get; set; }
    public double MoreThanTwoScore { get; set; }
    public double TwoToThreeScore { get; set; }
    public double NoScore { get; set; }
    public double HalfTimeScore { get; set; }
    public double HomeSideResult { get; set; }
    public double AwaySideResult { get; set; }
}

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
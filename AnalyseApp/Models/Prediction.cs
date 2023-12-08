using AnalyseApp.Enums;

namespace AnalyseApp.Models;

public record Prediction
{
    public DateTime Date { get; set; }
    public float HomeScore { get; set; }
    public float AwayScore { get; set; }
    public string Msg { get; init; } = default!;
    public PredictionType Type { get; set; }
    public double HomeWinAccuracy { get; set; }
    public double AwayWinAccuracy { get; set; }
    public double DrawAccuracy { get; set; }
    public double OverTwoGoalsAccuracy { get; set; }
    public double GoalGoalAccuracy { get; set; }
    public double TwoToThreeGoalsAccuracy { get; set; }
    public bool Qualified { get; init; }
}
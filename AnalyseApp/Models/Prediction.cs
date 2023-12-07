using AnalyseApp.Enums;

namespace AnalyseApp.Models;

public record Prediction
{
    public DateTime Date { get; set; }
    public float HomeScore { get; set; }
    public float AwayScore { get; set; }
    public string Msg { get; init; } = default!;
    public PredictionType Type { get; set; }
    public double Accuracy { get; set; }
    public bool Qualified { get; init; }
}
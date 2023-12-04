using AnalyseApp.Enums;

namespace AnalyseApp.models;

public record Prediction(BetType Type)
{
    public DateTime Date { get; set; }
    public float HomeScore { get; set; }
    public float AwayScore { get; set; }
    public string Msg { get; init; } = default!;
    public double Percentage { get; set; } = default!;
    public bool Qualified { get; init; }
}
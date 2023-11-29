using AnalyseApp.Enums;

namespace AnalyseApp.models;

public record Prediction(BetType Type)
{
    public string Msg { get; init; } = default!;
    public double Percentage { get; set; } = default!;
    public bool Qualified { get; init; }
};

public record GoalAnalysis(bool Qualified, QualificationType Type, BetType BetType);


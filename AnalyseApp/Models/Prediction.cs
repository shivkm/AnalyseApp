using AnalyseApp.Enums;

namespace AnalyseApp.models;

public record Prediction(double Percentage, BetType Type)
{
    public string Msg { get; init; } = default!;
    public double AwayPercentage { get; set; } = default!;
    public bool Qualified { get; init; }
    public bool IsHome { get; set; }
    public bool HeadToHeadIgnored { get; init; }
};

public record Percentage(double Total, double Home, double Away);
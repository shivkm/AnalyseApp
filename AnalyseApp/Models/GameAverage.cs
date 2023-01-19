namespace AnalyseApp.models;

public record MatchProbability
{
    public string Key { get; set; } = default!;
    public double Probability { get; set; }
    public double Bet365BookMaker { get; set; }
}
namespace PredictionTool.Models;

public record QualifiedGames(DateTime DateTime, string Home, string Away, string League)
{
    public string? Key { get; set; }
    public double Probability { get; set; }
}

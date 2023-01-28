namespace AnalyseApp.Models;

public record PoissonProbability
{
    public string Key { get; set; } = default!;
    public double Probability { get; set; }
}
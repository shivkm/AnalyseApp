namespace AnalyseApp.Models;

public record TeamProbability
{
    public string Key { get; set; } = default!;
    public double MonteCarloProbability { get; set; }
    public double PoisonProbability { get; set; }
    public Probability MarkovChainProbability { get; set; } = default!;
}

public record Probability(double Home, double Away, double Conceded = 0);


public record MarkovChainResult(string Key, double Probability);

public record SelfPoisonResult(string Key, double ScoreProbability, double ConcededProbability)
{
    public double MonteCarlo { get; set; }
    public double AwayMonteCarlo { get; set; }
};
namespace AnalyseApp.Models;

public record HighestProbability(Probability Suggestion, List<Probability> Probabilities);
public record Probability(string Type, double Percentage);
namespace PredictionTool.Models;

public record GameProbability(
    string Key,
    double PoissonProbability,
    double HomeMarkovChainScoreProbability,
    double AwayMarkovChainScoreProbability
);
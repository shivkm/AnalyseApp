namespace PredictionTool.Models;

public record TeamStrength(double TeamAttack, double TeamDefense, double LeagueScoredGoal, double LeagueConcededGaol);

public record MarkovChainResult(string Key, double Probability);
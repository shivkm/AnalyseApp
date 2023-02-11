namespace AnalyseApp.Models;

public record TeamStrength(
    double Attack,
    double Defense, 
    double TeamScoreAverage, 
    double TeamConcededAverage, 
    double LeagueScoredAverage, 
    double LeagueConcededAverage);
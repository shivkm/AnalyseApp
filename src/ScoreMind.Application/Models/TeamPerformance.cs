namespace AnalyseApp.Models;

public record TeamPerformance(
    double ScoringAverage,
    double ConcededAverage,
    double HalfTimeScoringAverage, 
    double HalfTImeConcededAverage,
    double ScoredShotsOnTarget,
    double ConcededShotsOnTarget
);
namespace PredictionTool.Models;

public record TeamAccuracy(
    double HomeScoredAvg, 
    double AwayScoredAvg, 
    double HomeConcededAvg, 
    double AwayConcededAvg, 
    double HomeScoreProbability, 
    double AwayScoreProbability,
    bool HomeLastFiveOver,
    bool AwayLastFiveOver,
    bool HomeLastFiveWon,
    bool AwayLastFiveWon
);
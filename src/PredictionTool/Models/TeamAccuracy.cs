namespace PredictionTool.Models;

public record TeamAccuracy(
    double HomeScoreProbability, 
    double AwayScoreProbability,
    double HomeHalftimeScoreProbability, 
    double AwayHalftimeScoreProbability,
    double HomeScoredGameAvg,
    double AwayScoredGameAvg,
    double HomeShotsOnGoalsAvg,
    double AwayShotsOnGoalsAvg,
    bool HomeLastFourOver,
    bool AwayLastFourOver,
    bool HomeLastThreeWon,
    bool AwayLastThreeWon
);
namespace AnalyseApp.Models;

public record GoalAverages(
    double LeagueHomeGoalAverage,
    double LeagueAwayGoalAverage,
    double HomeScoredGoalAverage,
    double HomeConcededGoalAverage,
    double AwayScoredGoalAverage,
    double AwayConcededGoalAverage)
{
    public double HomeAverage { get; set; }
    public double AwayAverage { get; set; }
};
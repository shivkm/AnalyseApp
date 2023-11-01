namespace AnalyseApp.Models;

public record TeamGoalAverage(
    GoalAverage Overall, 
    GoalAverage Recent
);

public record GoalAverage(
    double AvgGoalsScored, 
    double AvgGoalsConceded
);
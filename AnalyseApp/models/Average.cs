namespace AnalyseApp.models;

public record Average(
    decimal OneGoalPercentage,
    decimal TwoGoalPercentage, 
    decimal HalfTimePercentage,
    decimal TwoToThreeGoalGames,
    decimal WonGames,
    decimal ZeroZeroGames,
    decimal AllowedGoals,
    string? Msg
);
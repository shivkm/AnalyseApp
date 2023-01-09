namespace AnalyseApp.models;

public record Average(decimal OneGoalPercentage, decimal TwoGoalPercentage, decimal HalfTimePercentage, bool Qualified, string? Msg);
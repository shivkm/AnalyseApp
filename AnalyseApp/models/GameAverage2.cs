namespace AnalyseApp.models;

public record GameAverage2
{
    public double? HalfTimeWithOneGoal { get; set; }
    public double? OneGoal2 { get; set; }
    public double? TwoGoals { get; set; }
    public double? ZeroZero { get; set; }
    public double? TwoToThree { get; set; }
    public double? AllowGoal { get; set; }
    public Average? Home { get; set; }
    public Average? Away { get; set; }
    public Average? AllGames { get; set; }
    public decimal Won { get; set; }
    public decimal Draw { get; set; }
    public decimal Loss { get; set; }

    public override string ToString() =>
        $"OneGoal: {OneGoal2}, TwoGoals: {TwoGoals}, Zero Zero games: {ZeroZero}," +
        $" TwoToThree: {TwoToThree}, HalfTimeWithOneGoal: {HalfTimeWithOneGoal} ";
}

public record GameAverage
{
    public required Average Home { get; set; }
    public required Average Away { get; set; }
}
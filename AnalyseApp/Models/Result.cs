namespace AnalyseApp.models;

public record NextMatch
{
    public required  DateTime Date { get; set; }
    public required string HomeTeam { get; set; }
    public required string AwayTeam { get; set; }
    public GameAverage? HomeCurrentAverage { get; set; }
    public GameAverage? AwayCurrentAverage { get; set; }
    public GameAverage? HomeOverAllAverage { get; set; }
    public GameAverage? AwayOverAllAverage { get; set; }
    public Head2HeadAverage? CurrentHeadToHeadAverage { get; set; }
    public Head2HeadAverage? HeadToHeadAverage { get; set; }
    public string PredictATargetUsingALinearRegressionModel { get; set; }
    public string? Msg { get; set; }
    
    public override string ToString() =>
        $$"""Date: {{{Date}}, HomeTeam: {{HomeTeam}}, AwayTeam: {{AwayTeam}} HomeCurrentAverage: {{HomeCurrentAverage}} AwayCurrentAverage: {{AwayCurrentAverage}} HomeOverAllAverage: {{HomeOverAllAverage}} AwayOverAllAverage: {{AwayOverAllAverage}} CurrentHeadToHeadAverage: {{CurrentHeadToHeadAverage}} HeadToHeadAverage: {{HeadToHeadAverage}}} PredictATargetUsingALinearRegressionModel: {{PredictATargetUsingALinearRegressionModel}} """;
}


public record GameAverage
{
    public double? AtLeastOneGoal { get; set; }
    public double? MoreThanTwoGoals { get; set; }
    public double? ZeroZero { get; set; }
    public double? TwoToThree { get; set; }

    public override string ToString()
    {
        return $"At least one Goal: {AtLeastOneGoal}, More than two goals: {MoreThanTwoGoals}, Zero Zero games: {ZeroZero} " +
               $"Two to three goals {TwoToThree}";
    }
}

public record Head2HeadAverage
{
    public double? BothTeamScore { get; set; }
    public double? MoreThanTwoGoals { get; set; }
    public double? TwoToThree { get; set; }
    public double? ZeroZero { get; set; }
    public string? Hint { get; set; }
    
    public override string ToString()
    {
        return $"BothTeamScore: {BothTeamScore}, More than two goals: {MoreThanTwoGoals}, Zero Zero games: {ZeroZero} " +
               $"Two to three goals {TwoToThree} Hint: {Hint}";
    }
}
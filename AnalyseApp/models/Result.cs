namespace AnalyseApp.models;

public record NextMatch
{
    public required  DateTime Date { get; set; }
    public required string HomeTeam { get; set; }
    public required string AwayTeam { get; set; }
    public GameAnalysis? CurrentSeason { get; set; }
    public GameAnalysis? LastSixSeason { get; set; }
    public GameAnalysis? LastSixGames { get; set; }
}


public record GameAnalysis
{
    public GameAverage? HomeTeam { get; set; }
    public GameAverage? HomeTeamAtHomeField { get; set; }
    public GameAverage? AwayTeam { get; set; }
    public GameAverage? AwayTeamAtAwayField { get; set; }
    public Head2HeadAverage? HeadToHeadAverage { get; set; }
    public override string ToString() =>
        $"HomeTeam: {HomeTeam}, HomeTeamAtHomeField: {HomeTeamAtHomeField}, AwayTeam: {AwayTeam}," +
        $" AwayTeamAtAwayField: {AwayTeamAtAwayField} HeadToHeadAverage: {HeadToHeadAverage}";
}

public record GameAverage
{
    public double? HalfTimeWithOneGoal { get; set; }
    public double? OneGoal { get; set; }
    public double? TwoGoals { get; set; }
    public double? ZeroZero { get; set; }
    public double? TwoToThree { get; set; }
    public double? AllowGoal { get; set; }

    public override string ToString() =>
        $"OneGoal: {OneGoal}, TwoGoals: {TwoGoals}, Zero Zero games: {ZeroZero}," +
        $" TwoToThree: {TwoToThree}, HalfTimeWithOneGoal: {HalfTimeWithOneGoal} ";
    
}

public record Head2HeadAverage
{
    public double? BothTeamScore { get; set; }
    public double? MoreThanTwoGoals { get; set; }
    public double? TwoToThree { get; set; }
    public double? ZeroZero { get; set; }
    public double? GoalInFirstHalf { get; set; }
    public string? Hint { get; set; }
    
    public override string ToString()
    {
        return $"BothTeamScore: {BothTeamScore}, More than two goals: {MoreThanTwoGoals}, Zero Zero games: {ZeroZero} " +
               $"Two to three goals {TwoToThree} Hint: {Hint}";
    }
}



public record Game
{
    public string? Team { get; set; }
    public Result? BothTeamScore { get; set; }
    public Result? MoreThanTwoGoals { get; set; }
    public decimal TwoToThreeGoal { get; set; }
    public Result? OneGoalInFirstHalf { get; set; }
}

public record Result(decimal HomeTeam, decimal AwayTeam, decimal HeadToHead, bool Qualified, string Msg);
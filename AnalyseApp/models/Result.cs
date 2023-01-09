namespace AnalyseApp.models;

public record NextMatch2
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
    public GameAverage2? HomeTeam { get; set; }
    public GameAverage2? HomeTeamAtHomeField { get; set; }
    public GameAverage2? AwayTeam { get; set; }
    public GameAverage2? AwayTeamAtAwayField { get; set; }
    public Head2HeadAverage2? HeadToHeadAverage { get; set; }
    public override string ToString() =>
        $"HomeTeam: {HomeTeam}, HomeTeamAtHomeField: {HomeTeamAtHomeField}, AwayTeam: {AwayTeam}," +
        $" AwayTeamAtAwayField: {AwayTeamAtAwayField} HeadToHeadAverage: {HeadToHeadAverage}";
}



public record Head2HeadAverage2
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

public record Head2HeadAverage
{
    public decimal? BothTeamScore { get; set; }
    public decimal? MoreThanTwoGoals { get; set; }
    public decimal? TwoToThree { get; set; }
    public decimal? ZeroZero { get; set; }
    public decimal? GoalInFirstHalf { get; set; }
    public int TotalGames { get; set; }
    public string? Msg { get; set; }
    
    public override string ToString()
    {
        return $"BothTeamScore: {BothTeamScore}, More than two goals: {MoreThanTwoGoals}, Zero Zero games: {ZeroZero} " +
               $"Two to three goals {TwoToThree} Hint: {Msg}";
    }
}

public record Game
{
    public string? Title { get; set; }
    public Head2HeadAverage? HeadToHead { get; set; }
    public GameAverage? LastSixGames { get; set; }
    public GameAverage? LastTwelveGames { get; set; }
    public GameAverage? AllGame { get; set; }
    public string? Prediction { get; set; }
    
}

public record Result(decimal HomeTeam, decimal AwayTeam, decimal HeadToHead, bool Qualified, string Msg);
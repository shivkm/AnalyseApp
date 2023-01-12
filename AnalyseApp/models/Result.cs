namespace AnalyseApp.models;

public record UpComingGames
{
    public DateTime Date { get; set; }
    public required string HomeTeam { get; set; }
    public required string AwayTeam { get; set; }
}

public record Head2HeadAverage
{
    public string BothTeamScore { get; set; }
    public string MoreThanTwoGoals { get; set; }
    public string TwoToThree { get; set; }
    public string ZeroZero { get; set; }
    public string GoalInFirstHalf { get; set; }
    public bool BothTeamScoreQualified { get; set; }
    public bool MoreThanTwoGoalsQualified { get; set; }
    public bool TwoToThreeQualified { get; set; }
    public bool ZeroZeroQualified { get; set; }
    public bool GoalInFirstHalfQualified { get; set; }
    
    public override string ToString()
    {
        return $"BothTeamScore: {BothTeamScore}, More than two goals: {MoreThanTwoGoals}, Zero Zero games: {ZeroZero} " +
               $"Two to three goals {TwoToThree}";
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

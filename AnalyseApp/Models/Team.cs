namespace AnalyseApp.models;

public record Team(string Name, bool IsHome = default, bool Overall = default, bool HalfTimeGoal = default);


public record Average2
{
    public double? Home { get; set; }
    public double? Away { get; set; }
}

public record Average
{
    public Average2 ZeroZeroGameAverage { get; set; } = new();
    public Average2 OneSideResult { get; set; } = new();
    public Average2 ZeroOneResult { get; set; } = new();
    public Average2 ScoredGamesAverage { get; set; }= new();
    public Average2 ScoreThanTwoGoalsAverage { get; set; }= new();
    public Average2 HalftimeScoredGamesAverage { get; set; } = new();
    public Average2 HalftimeScoreAverage { get; set; } = new();
    public HeadToHead HeadToHeadGameAverage { get; set; } = new();
}

public record HeadToHead
{
    public int TotalGames { get; set; }
    public double BothTeamScore { get; set; }
    public double MoreThanTwoGoals { get; set; }
    public double TwoToThreeGoals { get; set; }
    public double NoGoal { get; set; }
    public double HalfTimeScored { get; set; }
    public double MoreThanTwoGoalAverage { get; set; }
    public double HalftimeScoreAverage { get; set; }
}

public record Game
{
    public string? Title { get; set; }
    public GameAverage SelfAnalysis { get; set; }
    public PoissonProbability PoissonProbability { get; set; }
}

public record GameAverage
{
    public Average? CurrentSeason { get; set; }
    public Average? LastSixMatches { get; set; }
    public Average? HistoricalMatches { get; set; }
    public PoissonProbability PoissonProbability { get; set; }
}
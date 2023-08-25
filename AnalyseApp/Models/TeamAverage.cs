namespace AnalyseApp.models;

public record TeamAverage(
    double ScoreAvg,
    double ScoreAvgAtHome,
    double ScoreAvgAtAway,
    double ExponentialMovingAvg
);

public record HeadToHeadData(
    int Count,
    double ScoreProbability,
    double OverScoredGames,
    double UnderScoredGames,
    double TwoToThreeGoalsGames,
    double BothTeamScoredGames,
    double ZeroScoredGames,
    double HomeTeamWon,
    double AwayTeamWon
)
{
    public Suggestion Suggestion { get; set; } = default!;
    
}


public record TeamData(
    int GamesCount,
    double ScoreProbability,
    double OverScoredGames,
    double UnderScoredGames,
    double TwoToThreeGoalsGames,
    double BothTeamScoredGames,
    double ZeroZeroGames,
    double HomeTeamWon,
    double AwayTeamWon,
    double WinAvg
)
{
    public Suggestion Suggestion { get; set; } = default!;
}


public record Suggestion(string Name, double Value);
using AnalyseApp.Enums;

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
    double ZeroScoredGoalGames,
    double MoreThanThreeGoalGames,
    double HomeTeamWon,
    double AwayTeamWon
)
{
    public Suggestion Suggestion { get; set; } = default!;
    
}


public record TeamData(
    int GamesCount,
    double TeamScoreProbability,
    double OverScoredGames,
    double UnderScoredGames,
    double TwoToThreeGoalsGames,
    double BothTeamScoredGames,
    double ZeroZeroGoalGamesAvg,
    double MoreThanThreeGoalGamesAvg,
    double HomeTeamWon,
    double AwayTeamWon,
    double WinAvg,
    double TeamScoredGames,
    double TeamAllowedGoalGames,
    BetType LastThreeMatchResult
)
{
    public Suggestion Suggestion { get; set; } = default!;
}


public record Suggestion(string Name, double Value);
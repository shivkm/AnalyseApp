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
    double? ScoreProbability,
    double OverScoredGames,
    double UnderTwoScoredGames,
    double TwoToThreeGoalsGames,
    double BothTeamScoredGames,
    double ZeroScoredGoalGames,
    double OverThreeGoalGames,
    double HomeTeamWon,
    double AwayTeamWon
)
{
    public Suggestion Suggestion { get; set; } = default!;
    
}


public record TeamData(
    int GamesCount,
    double? ScoringPower,
    double? HomeScoringPower,
    double? AwayScoringPower,
    double OverScoredGames,
    double UnderTwoScoredGames,
    double TwoToThreeGoalsGames,
    double BothTeamScoredGames,
    double ZeroZeroGoalGamesAvg,
    double OverThreeGoalGamesAvg,
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
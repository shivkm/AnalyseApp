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
    double HomeScoringPower,
    double AwayScoringPower,
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
    Goals Goals,
    int GamesCount,
    double? ScoringPower,
    double? ConcededPower,
    double? HomeScoringPower,
    double? HomeConcededPower,
    double? AwayScoringPower,
    double? AwayConcededPower,
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

public record Goals(int Scored, int Conceded, int HomeScoored, int HomeConceded, int AwayScored, int AwayConceded);

public record Suggestion(string Name, double Value);
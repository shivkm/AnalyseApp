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
    int GamesCount,
    TeamResult TeamResult,
    TeamOdds TeamOdds,
    TeamGoals TeamGoals,
    TeamGoals SeasonTeamGoals,
    double TeamScoredGames,
    double TeamConcededGoalGames,
    BetType LastThreeMatchResult
)
{
    public Suggestion Suggestion { get; set; } = default!;
}

public record MatchGoalsData(Goals Home, Goals Away);

public record TeamOdds(double HomeWin, double AwayWin, double Draw);
public record TeamResult(double OverTwoGoals, double BothScoredGoals, double TwoToThreeGoals, double UnderTwoGoals);
public record TeamGoals(Goals Total, Goals Home, Goals Away);
public record Goals(int MatchCount, int Scored, int Conceded, double ScoredAvg, double ConcededAvg, double ScoreProbability, double ConcededProbability);




public record Suggestion(string Name, double Value);


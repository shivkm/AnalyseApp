using AnalyseApp.Enums;
using AnalyseApp.Models;

namespace AnalyseApp.models;

public record TeamAverage(
    double ScoreAvg,
    double ScoreAvgAtHome,
    double ScoreAvgAtAway,
    double ExponentialMovingAvg
);

public record HeadToHeadData(
    int Count,
    double HomeProbability,
    double AwayProbability,
    double OverScoredGames,
    double UnderThreeScoredGames,
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
    GoalPower GoalPower,
    double TeamScoredGames,
    double TeamConcededGoalGames,
    LastThreeGameType LastThreeGameType
)
{
    public Suggestion Suggestion { get; set; } = default!;
}

public record MatchGoalsData(Goals Home, Goals Away);

public record TeamOdds(double HomeWin, double AwayWin, double Win, double Loss, double Draw);
public record TeamResult(double OverTwoGoals, double BothScoredGoals, double TwoToThreeGoals,
    double UnderThreeGoals, double NoGoalGameAvg, double AtLeastOneGoalGameAvg, double UnderFourGoalsGameAvg);
public record TeamGoals(Goals Total, Goals Home, Goals Away);
public record Goals(int MatchCount, int Scored, int Conceded, double ScoredAvg, double ConcededAvg, double ScoreProbability, double ConcededProbability);




public record Suggestion(string Name, double Value);


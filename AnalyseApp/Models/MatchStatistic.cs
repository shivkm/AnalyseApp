namespace AnalyseApp.models;

public record MatchStatistic(
    TeamStatistic HomeMatch,
    TeamStatistic AwayMatch,
    TeamStatistic HeadToHead,
    bool Result
);

public record TeamStatistic(
    Average Scored,
    Average ConcededScored,
    Average OneSidedScored, 
    Average OneSidedConceded, 
    Average ZeroZeroScored, 
    Average TwoToThreeGoalScored,
    Average OverTwoGoalsScored,
    Average OverThreeGoalsScored,
    Average BothTeamScoredGoals,
    double HomeSideProbability,
    double AwaySideProbability
);

public record Average(
    double HomeSideAvg, 
    double AwaySideAvg, 
    int LatestMatches, 
    string? LastThreeResult = null
);
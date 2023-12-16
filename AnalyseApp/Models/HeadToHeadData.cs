namespace AnalyseApp.Models;

public record HeadToHeadData(
    int Count,
    bool ScoredHomeGoal,
    bool ScoredAwayGoal,
    bool NoScoredInLastMatch,
    bool NoScoredInLastTwoMatch,
    bool ScoredInThirdAndLastMatch
    // bool Over, 
    // bool GoalGoal, 
    // bool HomeWin, 
    // bool AwayWin
    );
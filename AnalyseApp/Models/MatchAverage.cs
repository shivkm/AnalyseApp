namespace AnalyseApp.Models;

public record MatchAverage(string Match, float HomeAverage, float AwayAverage, DateTime PlayedOn)
{
    public bool OverUnder { get; set; } // True for 'over', false for 'under'
    public bool GoalGoal { get; set; } // True for 'goal-goal', false for 'no-goal'
};
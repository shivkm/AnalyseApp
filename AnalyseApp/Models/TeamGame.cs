namespace AnalyseApp.Models;

public record TeamGame
{
    public int GamePlayed { get; set; }
    public int NoGoal { get; set; }
    public int OneSide { get; set; }
    public int OneSideWin { get; set; }
    public int HalftimeGoal { get; set; }
    public int HalftimeGoalScored { get; set; }
    public int HalftimeGoalConceded { get; set; }
}
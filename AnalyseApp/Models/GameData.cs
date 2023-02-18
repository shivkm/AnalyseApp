namespace AnalyseApp.Models;

public class GameData
{
    public string Name { get; set; }
    public double HalftimeGoalAverage { get; set; }
    public double GoalsGameAverage { get; set; }
    public double ZeroOneGameAverageByTeam { get; set; }
    public double ZeroOneGameAverage { get; set; }
    public double ZeroZeroGameAverage { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
}

public class HeadToHeadData
{
    // Define properties to store relevant data for the head-to-head record between two teams
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public int MatchesPlayed { get; set; }
    public double HomeWins { get; set; }
    public double AwayWins { get; set; }
    public double Draws { get; set; }
    public double HomeHalftimeGoalsScored { get; set; }
    public double AwayHalftimeGoalsScored { get; set; }
    public double HomeGoalsGameAverage { get; set; }
    public double AwayGoalsGameAverage { get; set; }
    public double ZeroOneHomeGameAverage { get; set; }
    public double ZeroOneAwayGameAverage { get; set; }
    public double ZeroZeroGameAverage { get; set; }
    // Add more properties as needed
}

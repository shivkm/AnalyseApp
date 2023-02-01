namespace AnalyseApp.Models;

public record TeamData
{
    public string? Title { get; set; }
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string? ProbabilityKey { get; set; }
    public double? Probability { get; set; }
    public double? AverageAndProbability { get; set; }
    public Average? NoGoalGames { get; set; }
    public Average? BothScoreGames { get; set; }
    public Average? MoreThanTwoGoalsGames { get; set; }
    public Average? TwoToThreeGoalGames { get; set; }
    public Average? HalftimeGoalGames { get; set; }
    public Average? OneSideGoalGames { get; set; }
    public Average? Win { get; set; }
}

public record NextGame
{
    public Team HomeHome { get; set; } = default!;
    public Team HomeAway { get; set; } = default!;
    public Team AwayHome { get; set; } = default!;
    public Team AwayAway { get; set; } = default!;
    public HeadToHead HeadToHead { get; set; } = default!;
}

public record HeadToHead
{
    public int GamesPlayed { get; set; }
    public double BothTeamScored { get; set; }
    public double MoreThanTwoScored { get; set; }
    public double TwoToThreeScored { get; set; }
    public double NoScored { get; set; }
    public double HalfTimeScored { get; set; }
    public double HomeSideScored { get; set; }
    public double AwaySideScored { get; set; }
    public double HomeWin { get; set; }
    public double AwayWin { get; set; }
    public double Draw { get; set; }
}

public record Team
{
    public int GamesPlayed { get; set; }
    public double GoalsConceded { get; set; }
    public double HalftimeGoalsScored { get; set; }
    public double HalftimeGoalsConceded { get; set; }
    public double NoGoalGames { get; set; }
    public double GoalsScored { get; set; }
    public double BothScoreGames { get; set; }
    public double MoreThanTwoGoalsGames { get; set; }
    public double TwoToThreeGoalGames { get; set; }
    public double HalftimeGoalGames { get; set; }
    public double OneSideGoalGames { get; set; }
    public double WinOneSideGoalGames { get; set; }
    public double Win { get; set; }
    public double Loss { get; set; }
    public double Draw { get; set; }
}
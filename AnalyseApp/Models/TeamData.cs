namespace AnalyseApp.Models;

public record TeamData
{
    public string? Team { get; set; }
    public double GoalAverage { get; set; }
    public bool OneGoalQualified { get; set; }
    public bool ConcededQualified { get; set; }
    public double ConcededAverage { get; set; }
    public double HalftimeGoalAverage { get; set; }
    public bool HalftimeOneGoalQualified { get; set; }
    public double HalftimeConcededAverage { get; set; }
    public double ZeroGoalAverage { get; set; }
    public bool LastHomeGameZeroZero { get; set; }
    public bool LastAwayGameZeroZero { get; set; }
}

public record NextGame
{
    public Team Home { get; set; } = default!;
    public Team Away { get; set; } = default!;
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
    public double LessThanThreeGoal { get; set; }
    public bool LastHomeGameZeroZero { get; set; }
    public bool LastAwayGameZeroZero { get; set; }
    public double HomeWin { get; set; }
    public double AwayWin { get; set; }
    public double Draw { get; set; }
}

public record Team
{
    public int GamesPlayed { get; set; }
    public double AllowGoals { get; set; }
    public double HalftimeGoalsScored { get; set; }
    public double HalftimeGoalsConceded { get; set; }
    public double NoGoalGames { get; set; }
    public double ScoredGoal { get; set; }
    public double BothScoreGames { get; set; }
    public double MoreThanTwoGoalsGames { get; set; }
    public double TwoToThreeGoalGames { get; set; }
    public double HalftimeGoalGames { get; set; }
    public double OneSideGoalGames { get; set; }
    public double LessThanThreeGoalsAccuracy { get; set; }
    public double LastTenGamesWinAccuracy { get; set; }
    public double LastTenGamesOverTwoGoalsAccuracy { get; set; }
    public double LastTenGamesDrawAccuracy { get; set; }
    public bool LastFiveGamesLess { get; set; }
    public bool LastFiveGamesOver { get; set; }
    public bool LastSixGamesBothScored { get; set; }
    public bool LastTwoGamesWithZeroGoal { get; set; }
    public bool LastTwoGamesLessThanTwoGoals { get; set; }
    public double WinAccuracy { get; set; }
    public double LossAccuracy { get; set; }
    public double DrawAccuracy { get; set; }
}
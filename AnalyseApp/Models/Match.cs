using CsvHelper.Configuration.Attributes;

namespace AnalyseApp.models;

public record Match
{
    [Name("Div")]
    public string League { get; set; }
    [Name("Date")]
    public string Date { get; set; }
    [Optional, Name("Time")]
    public string Time { get; set; }
    [Name("HomeTeam")]
    public string HomeTeam { get; set; }
    [Name("AwayTeam")]
    public string AwayTeam { get; set;  }
    [Default(0), Name("FTHG")]
    public float FullTimeHomeGoals { get; set; }
    [Default(0), Name("FTAG")]
    public float FullTimeAwayGoals { get; set; }
    [Default(0), Name("HTHG")]
    public float HalfTimeHomeGoals { get; set; }
    [Default(0), Name("HTAG")]
    public float HalfTimeAwayGoals { get; set; }
    [Ignore] public bool IsOverTwoGoals => FullTimeHomeGoals + FullTimeAwayGoals > 2;
    [Ignore] public bool GoalGoal => FullTimeHomeGoals > 0 && FullTimeAwayGoals > 0;
    [Ignore] public bool TwoToThreeGoals => FullTimeHomeGoals + FullTimeAwayGoals is 2 or 3;
    [Ignore] public bool HomeTeamWin => FullTimeHomeGoals > FullTimeAwayGoals;
    [Ignore] public bool AwayTeamWin => FullTimeAwayGoals > FullTimeHomeGoals;
}
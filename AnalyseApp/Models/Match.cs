using CsvHelper.Configuration.Attributes;

namespace AnalyseApp.Models;

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
    [Ignore] public float AverageAwayGoals { get; set; }
    [Ignore] public float AverageHomeGoals { get; set; }
    [Ignore] public string Outcome { get; set; }
    [Ignore] public string OverTwoGoals { get; set; }
    [Ignore] public string GoalGoals { get; set; }
    [Ignore] public string TwoToThreeGoals { get; set; }
}
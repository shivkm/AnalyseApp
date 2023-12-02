using CsvHelper.Configuration.Attributes;

namespace AnalyseApp.models;

public record Match
{
    public string Div { get; set; }
    public string Date { get; set; }
    [Optional]
    public string Time { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public int? FTHG { get; set; }
    public int? FTAG { get; set; }
    public int? HTHG { get; set; }
    public int? HTAG { get; set; }
    
    [Ignore]
    public bool AfterSummerBreak { get; set; }
    
    [Ignore]
    public bool AfterWinterBreak { get; set; }
}

public class TeamAnalysis
{
    public string TeamName { get; set; }
    public double AvgGoalsScored { get; set; }
    public double AvgGoalsConceded { get; set; }
}
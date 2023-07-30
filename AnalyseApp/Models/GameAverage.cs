using CsvHelper.Configuration.Attributes;

namespace AnalyseApp.models;

public class GameAverage
{
    [Name("StringBeg")]
    public string Name { get; set; }
    [Name("Liga")]
    public string League { get; set; }
    [Name("Tore Heimteam")]
    public int HomeGoal { get; set; }
    [Name("Tore Auswaertsteam")]
    public int AwayGoal { get; set; }
    [Name("Avg. HGS")]
    public double HomeGoalScored { get; set; }
    [Name("Avg. HGC")]
    public double HomeGoalConceded { get; set; }
    [Name("Avg. AGS")]
    public double AwayGoalScored { get; set; }
    [Name("Avg. AGC")]
    public double AwayGoalConceded { get; set; }
    [Name("Avg. HGS 6FH")]
    public double HomeHomeGoalScored { get; set; }
    [Name("Avg. HGC 6FH")]
    public double HomeHomeGoalConceded { get; set; }
    [Name("Avg. AGS 6FH")]
    public double HomeAwayGoalScored { get; set; }
    [Name("Avg. AGC 6FH")]
    public double HomeAwayGoalConceded { get; set; }
    [Name("Avg. AGC 6FA")]
    public double AwayHomeGoalScored { get; set; }
    [Name("Avg. AGC 6FA")]
    public double AwayHomeGoalConceded { get; set; }
    [Name("Avg. AGC 6FA")]
    public double AwayAwayGoalScored { get; set; }
    [Name("Avg. AGC 6FA")]
    public double AwayAwayGoalConceded { get; set; }
    [Name("Avg. HGS 6FH2H"), Optional]
    public double? HeadToHeadHomeGoalScored { get; set; }
    [Name("Avg. HGC 6FH2H"), Optional]
    public double? HeadToHeadHomeGoalConceded { get; set; }
    [Name("Avg. AGS 6FH2H"), Optional]
    public double? HeadToHeadAwayGoalScored { get; set; }
    [Name("Avg. AGC 6FH2H"), Optional]
    public double? HeadToHeadAwayGoalConceded { get; set; }
}
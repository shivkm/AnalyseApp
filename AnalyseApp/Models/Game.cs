using CsvHelper.Configuration.Attributes;

namespace AnalyseApp.models;

public class Game
{
    public string League { get; set; }
    public string Date { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    [Optional]
    public int? FullTimeGoal { get; set; }
    [Optional]
    public int? HalfTimeGoal { get; set; }
}
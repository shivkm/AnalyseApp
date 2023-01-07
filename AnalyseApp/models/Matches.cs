using CsvHelper.Configuration.Attributes;
using Microsoft.ML.Data;

namespace AnalyseApp.models;

public class Matches
{
    public string Date { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public int? FTHG { get; set; }
    public int? FTAG { get; set; }
    public string FTR { get; set; }
    public int? HTHG { get; set; }
    public int? HTAG { get; set; }
    public string HTR { get; set; }
}

class GameData
{
    public float HomeGoals { get; set; }
    public float AwayGoals { get; set; }
    public float HalfTimeHomeGoals { get; set; }
    public float HalfTimeAwayGoals { get; set; }
    public float Label { get; set; }
}

class DataPoint
{
    public float[] Features { get; set; }
    public float Label { get; set; }
}

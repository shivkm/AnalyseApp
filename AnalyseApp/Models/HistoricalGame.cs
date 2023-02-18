using CsvHelper.Configuration.Attributes;
using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record HistoricalGame
{
    public string Div { get; set; } = default!;
    public string Date { get; set; } = default!;
    [Optional]
    public string Time { get; set; } = default!;
    public string HomeTeam { get; set; } = default!;
    public string AwayTeam { get; set; } = default!;
    public int? FTHG { get; set; } = default!;
    public int? FTAG { get; set; } = default!;
    public string FTR { get; set; } = default!;
    public int? HTHG { get; set; } = default!;
    public int? HTAG { get; set; } = default!;
    public string HTR { get; set; } = default!;
    [Optional]
    public int? HS { get; set; }
    [Optional]
    public int? AS { get; set; }
    [Optional]
    public int? HST { get; set; }
    [Optional]
    public int? AST { get; set; }
    [Optional]
    public int? HO { get; set; }
    [Optional]
    public int? AO { get; set; }
    [Optional]
    public int? HF { get; set; }
    [Optional]
    public int? AF { get; set; }
    [Optional]
    public string HomeWin { get; set; } = default!;
    [Optional]
    public string Draw { get; set; } = default!;
    [Optional]
    public string AwayWin { get; set; } = default!;
    [Optional]
    public string MoreThanTwoGoals { get; set; } = "0";
    [Optional]
    public string LessThanTwoGoals { get; set; } = "0"!;
    [Optional]
    public string BothTeamScore { get; set; } = "0"!;
    [Optional]
    public string TwoToThree { get; set; } = "0"!;
}

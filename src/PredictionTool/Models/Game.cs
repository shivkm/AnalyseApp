using CsvHelper.Configuration.Attributes;

namespace PredictionTool.Models;

public record Game
{
    public DateTime DateTime { get; set; }
    public string League { get; set; } = default!;
    public int GameDay { get; set; }
    public string Home { get; set; } = default!;
    public string Away { get; set; } = default!;
    public int? FullTimeHomeScore { get; set; }
    public int? FullTimeAwayScore { get; set; }
    public int? HalftimeHomeScore { get; set; }
    public int? HalftimeAwayScore { get; set; }
    public string? FullTimeResult { get; set; }
}


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
}
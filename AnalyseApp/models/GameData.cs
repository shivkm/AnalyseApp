namespace AnalyseApp.models;

public record GameData
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




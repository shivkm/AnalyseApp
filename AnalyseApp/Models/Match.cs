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
   
    [Optional,Default(0), Name("HS")]    
    public float HomeShots { get; set; }
    
    [Optional, Default(0), Name("AS")]    
    public float AwayShots { get; set; }
    
    [Optional, Default(0), Name("HST")]    
    public float HomeTargetShots { get; set; }
    
    [Optional, Default(0), Name("AST")]    
    public float AwayTargetShots { get; set; }
}
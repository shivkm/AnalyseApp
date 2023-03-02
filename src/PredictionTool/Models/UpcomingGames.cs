namespace PredictionTool.Models;

public record Filters
{
    public int Season { get; set; }
    public string Matchday { get; set; }
}

public record Season
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CurrentMatchday { get; set; }
    public string Winner { get; set; }
}

public record Team
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
}


public record Match
{
    public Season Season { get; set; }
    public int Id { get; set; }
    public DateTime UtcDate { get; set; }
    public string Status { get; set; }
    public int Matchday { get; set; }
    public DateTime LastUpdated { get; set; }
    public Team HomeTeam { get; set; }
    public Team AwayTeam { get; set; }
}

public record MatchesResponse
{
    public Filters Filters { get; set; }
    public List<Match>? Matches { get; set; }
}
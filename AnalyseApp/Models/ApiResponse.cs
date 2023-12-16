using Newtonsoft.Json;

namespace AnalyseApp.Models;

public class ApiResponse
{
    [JsonProperty("response")]
    public List<LeagueData> Response { get; set; }
}

public class LeagueData
{
    public League League { get; set; }
    public Country Country { get; set; }
}

public class League
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Logo { get; set; }
}

public class Country
{
    public string Name { get; set; }
    public string Code { get; set; }
    public string Flag { get; set; }
}


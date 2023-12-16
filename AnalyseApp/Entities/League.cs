namespace AnalyseApp.Entities;

public record League
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Logo { get; set; }
    public string CountryName { get; set; }
    public string CountryCode { get; set; }
}
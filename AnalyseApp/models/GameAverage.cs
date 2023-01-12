namespace AnalyseApp.models;

public record GameAverage
{
    public Average Home { get; set; }
    public Average Away { get; set; }
   
    public override string ToString()
        => $"Home: {Home}, Away: {Away}";
}
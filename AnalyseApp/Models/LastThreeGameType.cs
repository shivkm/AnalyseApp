namespace AnalyseApp.Models;

public record LastThreeGameType(Average Highest, List<Average> Averages);
public record Average(string Type, int Count);
namespace AnalyseApp.Models;

public record PoissonProbability
{
    public string Key { get; set; } = default!;
    public double Probability { get; set; }
    public double MoreThanTwoScore { get; set; }
    public double BothTeamScore { get; set; }
    public double ZeroZeroScore { get; set; }
    public double TwoToThreeScore { get; set; }
    public double LessThanTwoScore { get; set; }
}
namespace AnalyseApp.models;

public record NextGame(
    int TakeLastGames,
    int ExpectedPercentageForOneGoal, 
    int ExpectedPercentageForTwoGoal
)
{
    public required string Team { get; set; }
    public required string Msg { get; set; }
    public bool IsHome { get; set; }
};
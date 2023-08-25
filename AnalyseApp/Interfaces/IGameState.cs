namespace AnalyseApp.Interfaces;

public interface IGameState
{
    void CalculateOverallPoissonProbability(string home, string away, string playedOn);
    void CalculateCurrentPoissonProbability(string home, string away, string playedOn);
    void GenerateGameTeamData(string home, string away, string playedOn);
    string PerformAction(string home, string away, string playedOn);
}
using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IMatchPredictor
{
    void Execute();
    Prediction Execute(string home, string away, string playedOn);
}
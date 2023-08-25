using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IMatchPredictor
{
    Prediction Execute(string home, string away, string playedOn);
}
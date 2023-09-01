using AnalyseApp.Enums;
using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IMatchPredictor
{
    void Execute();
    double GetPredictionAccuracyRate(string fixtureName);
    Prediction Execute(string home, string away, string playedOn, BetType? betType = BetType.Unknown);
}
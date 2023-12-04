using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IMatchPredictor
{
    List<Prediction> GenerateRandomPredictionsBy(int gameCount, string fixture = "fixtures.csv");
    void GenerateFixtureFiles(string fixtureName);
    Prediction Execute(Match match);
}
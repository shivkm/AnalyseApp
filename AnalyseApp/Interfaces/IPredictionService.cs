using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IPredictionService
{
    List<Prediction> GenerateRandomPredictionsBy(int gameCount, string fixture = "fixtures.csv");
    void GenerateFixtureFiles(string fixtureName);
}
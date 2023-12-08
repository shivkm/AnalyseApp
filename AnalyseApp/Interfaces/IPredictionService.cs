using AnalyseApp.Enums;
using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IPredictionService
{
    List<Prediction> GenerateRandomPredictionsBy(int gameCount, PredictionType type = PredictionType.OverTwoGoals, 
        double probability = 0.0, string fixture = "fixtures.csv");
    void GenerateFixtureFiles(string fixtureName);
}
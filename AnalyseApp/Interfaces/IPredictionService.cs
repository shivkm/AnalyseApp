using AnalyseApp.Enums;
using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IPredictionService
{
    List<Prediction> GenerateRandomPredictionsBy(int gameCount, 
        DateTime playingOn,
        bool dayPredictions,
        PredictionType type = PredictionType.OverTwoGoals, 
        string fixture = "fixtures.csv");
    void GenerateFixtureFiles(string fixtureName);
}
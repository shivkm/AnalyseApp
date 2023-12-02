using AnalyseApp.models;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Interfaces;

public interface IMachineLearning
{
    IDataView PrepareDataBy(List<Match> matches);
    (ITransformer transformer, DataOperationsCatalog.TrainTestData trainTestData ) TrainModel(IDataView dataView);
    double EvaluateModel(ITransformer model, IDataView testData, string type);
    bool PredictOutcome(SoccerGameData gameData, ITransformer model, string type);
}
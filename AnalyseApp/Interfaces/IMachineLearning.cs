using AnalyseApp.models;
using Microsoft.ML;

namespace AnalyseApp.Interfaces;

public interface IMachineLearning
{
    IDataView PrepareDataBy(IEnumerable<Match> historicalMatches);
    ITransformer TrainModel(IDataView dataView, string type);
    (IDataView trainSet, IDataView testSet) SplitData(IDataView dataView);
    void SaveModel(ITransformer model, IDataView trainingDataView, string modelPath);
    ITransformer LoadModel(string modelPath);
    double EvaluateModel(ITransformer model, IDataView testData, string type);
    bool PredictOutcome(Match newMatch, ITransformer model, string type);
}
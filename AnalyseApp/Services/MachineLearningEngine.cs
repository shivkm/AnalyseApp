using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class MachineLearningEngine: IMachineLearningEngine
{
    private readonly MLContext _mlContext = new();
    
    public IDataView PrepareDataBy(IEnumerable<Match> historicalMatches)
    {
        // Load data into IDataView
        var dataView = _mlContext.Data.LoadFromEnumerable(historicalMatches);
        return dataView; 
    }
    
    public (IDataView trainSet, IDataView testSet) SplitData(IDataView dataView)
    {
        var splitData = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
        return (splitData.TrainSet, splitData.TestSet);
    }

    public ITransformer TrainModel(IDataView dataView, string type)
    {
        // Define the training pipeline
        var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding("HomeTeamEncoded", "HomeTeam")
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("AwayTeamEncoded", "AwayTeam"))
            .Append(_mlContext.Transforms.Concatenate("Features", "HomeTeamEncoded", "AwayTeamEncoded", "FullTimeHomeGoals", "FullTimeAwayGoals", "HalfTimeHomeGoals", "HalfTimeAwayGoals"))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: type));

        var model = pipeline.Fit(dataView);

        return model;
    }
    
    public double EvaluateModel(ITransformer model, IDataView testData, string type)
    {
        var predictions = model.Transform(testData);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, type);

        return metrics.Accuracy;
    }
    
    public void SaveModel(ITransformer model, IDataView trainingDataView, string modelPath)
    {
        _mlContext.Model.Save(model, trainingDataView.Schema, modelPath);
    }


    public ITransformer LoadModel(string modelPath) =>  _mlContext.Model.Load(modelPath, out _);
    
    
    public bool PredictOutcome(Match newMatch, ITransformer model, string type)
    {
        var predictionFunction = _mlContext.Model
                .CreatePredictionEngine<Match, MatchPrediction>(model);
            
        var prediction = predictionFunction.Predict(newMatch);
        return prediction.Prediction;
    }
}
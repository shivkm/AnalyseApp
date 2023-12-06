using AnalyseApp.Interfaces;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class MachineLearningEngine: IMachineLearningEngine
{
    private IDataView _dataView = default!;
    private readonly MLContext _mlContext = new();
    private List<Match> _historicalData = default!;
    private DataOperationsCatalog.TrainTestData _trainTestData;
    
    public void PrepareDataBy(IEnumerable<Match> historicalData)
    {
        historicalData = historicalData.Select(item =>
        {
            string outcome, goals;
            if (item.FullTimeHomeGoals > item.FullTimeAwayGoals)
                outcome = "Win";
            else if (item.FullTimeHomeGoals < item.FullTimeAwayGoals)
                outcome = "Lose";
            else
                outcome = "Draw";
            
            return item with { Outcome = outcome };
        }).ToList();
        
        _dataView = _mlContext.Data.LoadFromEnumerable(historicalData);
    }
    
    public ITransformer TrainModel(string? type)
    {
        var pipeline = _mlContext.Transforms.Conversion
            .MapValueToKey(inputColumnName: "Outcome", outputColumnName: "Label")
            .Append(_mlContext.Transforms.Categorical
                .OneHotEncoding("HomeTeamEncoded", nameof(Match.HomeTeam)))
            .Append(_mlContext.Transforms.Categorical
                .OneHotEncoding("AwayTeamEncoded", nameof(Match.AwayTeam)))
            .Append(_mlContext.Transforms.Categorical
                .OneHotEncoding("LeagueEncoded", nameof(Match.League)))
            .Append(_mlContext.Transforms.Concatenate(
                "Features", 
                "HomeTeamEncoded", "AwayTeamEncoded", "LeagueEncoded",
                nameof(Match.FullTimeHomeGoals), nameof(Match.FullTimeAwayGoals),
                nameof(Match.HalfTimeHomeGoals), nameof(Match.HalfTimeAwayGoals),
                nameof(Match.AverageHomeGoals), nameof(Match.AverageAwayGoals))
                )
            .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
        
        _trainTestData = _mlContext.Data.TrainTestSplit(_dataView, testFraction: 0.2);
        var model = pipeline.Fit(_trainTestData.TrainSet);

        return model;
    }
    
    public void SaveModel(ITransformer model, string modelPath)
    {
        _mlContext.Model.Save(model, _dataView.Schema, modelPath);
    }


    public ITransformer LoadModel(string modelPath) =>  _mlContext.Model.Load(modelPath, out _);
    
    
    public double EvaluateModel(ITransformer model, string? type)
    {
        var predictions = model.Transform(_trainTestData.TestSet);
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);


        return metrics.MicroAccuracy;
    }
    
    public string PredictOutcome(Match match, ITransformer model, string? type)
    {
        var predictionFunction = _mlContext.Model
            .CreatePredictionEngine<Match, OddPrediction>(model);
            
        var prediction = predictionFunction.Predict(match);
        return prediction.Outcome;
    }
}
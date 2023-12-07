using AnalyseApp.Enums;
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
            var outcome = item.FullTimeHomeGoals > item.FullTimeAwayGoals 
                ? PredictionType.HomeWin.ToString() 
                : item.FullTimeHomeGoals < item.FullTimeAwayGoals 
                    ? PredictionType.AwayWin.ToString() 
                    : PredictionType.Draw.ToString();
            
            var goalGoals = item is { FullTimeHomeGoals: > 0, FullTimeAwayGoals: > 0 } 
                ? "yes" 
                : "no";
            
            var overTwoGoals = item.FullTimeHomeGoals + item.FullTimeAwayGoals > 2 
                ? "yes" 
                : "no";
            
            var twoToThreeGoals = (item.FullTimeHomeGoals + item.FullTimeAwayGoals) == 2 ||
                                        (item.FullTimeHomeGoals + item.FullTimeAwayGoals) ==  3 
                ? "yes" 
                : "no";
            
            return item with
            {
                Outcome = outcome, GoalGoals = goalGoals, OverTwoGoals = overTwoGoals, TwoToThreeGoals = twoToThreeGoals,
                FullTimeHomeGoals = 0, FullTimeAwayGoals = 0, HalfTimeHomeGoals = 0, HalfTimeAwayGoals = 0
            };
            
        }).ToList();
        
        _dataView = _mlContext.Data.LoadFromEnumerable(historicalData);
        _trainTestData = _mlContext.Data.TrainTestSplit(_dataView, testFraction: 0.2);
    }
    
    public ITransformer TrainModel(PredictionType type)
    {
        var labelColumns = DetermineLabelColumnName(type);
        var featureColumns = DetermineFeatureColumns(type);
        
        var pipeline = _mlContext.Transforms.Conversion
            .MapValueToKey(inputColumnName: labelColumns, outputColumnName: "Label")        
            .Append(_mlContext.Transforms.Categorical
                .OneHotEncoding("HomeTeamEncoded", nameof(Match.HomeTeam)))
            .Append(_mlContext.Transforms.Categorical
                .OneHotEncoding("AwayTeamEncoded", nameof(Match.AwayTeam)))
            .Append(_mlContext.Transforms.Categorical
                .OneHotEncoding("LeagueEncoded", nameof(Match.League)))
            .Append(_mlContext.Transforms.Concatenate("Features", featureColumns) )
            .Append(_mlContext.MulticlassClassification.Trainers.LightGbm())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
        
        var model = pipeline.Fit(_trainTestData.TrainSet);

        return model;
    }
    
    public void SaveModel(ITransformer model, string modelPath)
    {
        _mlContext.Model.Save(model, _dataView.Schema, modelPath);
    }


    public ITransformer LoadModel(string modelPath) =>  _mlContext.Model.Load(modelPath, out _);
    
    
    public double EvaluateModel(ITransformer model, PredictionType type)
    {
        var predictions = model.Transform(_trainTestData.TestSet);
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);
        
        return metrics.MicroAccuracy;
    }
    
    public PredictionType PredictOutcome(Match match, ITransformer model, PredictionType type)
    {
        if (type is PredictionType.OverTwoGoals or PredictionType.UnderTwoGoals)
        {
            var overTwoGoalsPredictionFunction = _mlContext.Model
                .CreatePredictionEngine<Match, OverUnderPrediction>(model);
            
            var overTwoGoalsPrediction = overTwoGoalsPredictionFunction.Predict(match);
            return overTwoGoalsPrediction.OverUnderGoals == "yes" 
                ? PredictionType.OverTwoGoals 
                : PredictionType.UnderTwoGoals;
        }
        
        if (type is PredictionType.Draw or PredictionType.HomeWin or PredictionType.AwayWin)
        {
            var predictionFunction = _mlContext.Model
                .CreatePredictionEngine<Match, OddPrediction>(model);
            
            var prediction = predictionFunction.Predict(match);
            
            return prediction.Outcome == PredictionType.HomeWin.ToString()
                ? PredictionType.HomeWin 
                : prediction.Outcome == PredictionType.AwayWin.ToString()
                    ? PredictionType.AwayWin 
                    :  PredictionType.Draw;
        }
        
        if (type is PredictionType.GoalGoals)
        {
            var goalGoalPredictionFunction = _mlContext.Model
                .CreatePredictionEngine<Match, GoalGoalsPrediction>(model);
            
            var goalGoalPrediction = goalGoalPredictionFunction.Predict(match);
            return goalGoalPrediction.GoalGoals == "yes"
                ? PredictionType.GoalGoals 
                : PredictionType.Unknown;
        }
        
        var twoToThreePredictionFunction = _mlContext.Model
            .CreatePredictionEngine<Match, TwoToThreeGoalsPrediction>(model);
            
        var twoToThreePrediction = twoToThreePredictionFunction.Predict(match);
        return twoToThreePrediction.TwoToThreeGoals == "yes" 
            ? PredictionType.TwoToThreeGoals 
            : PredictionType.Unknown;
    }
    
    private static string[] DetermineFeatureColumns(PredictionType type)
    {
        switch (type)
        {
            case PredictionType.OverTwoGoals:
            case PredictionType.UnderTwoGoals:
            case PredictionType.HomeWin:
            case PredictionType.AwayWin:
            case PredictionType.Draw:
            case PredictionType.GoalGoals:
            case PredictionType.TwoToThreeGoals:
                return new[] { "HomeTeamEncoded", "AwayTeamEncoded", "LeagueEncoded", "AverageAwayGoals", "AverageHomeGoals"};
            default:
                throw new ArgumentException("Invalid prediction type");
        }
    }

    
    private static string DetermineLabelColumnName(PredictionType type)
    {
        return type switch
        {
            PredictionType.HomeWin => nameof(Match.Outcome),
            PredictionType.AwayWin => nameof(Match.Outcome),
            PredictionType.Draw => nameof(Match.Outcome),
            PredictionType.OverTwoGoals => nameof(Match.OverTwoGoals),
            PredictionType.UnderTwoGoals => nameof(Match.OverTwoGoals),
            PredictionType.GoalGoals => nameof(Match.GoalGoals),
            PredictionType.TwoToThreeGoals => nameof(Match.TwoToThreeGoals),
            _ => throw new ArgumentException("Invalid prediction type")
        };
    }

}
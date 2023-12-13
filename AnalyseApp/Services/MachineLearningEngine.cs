using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class MachineLearningEngine: IMachineLearningEngine
{
    private IDataView _dataView = default!;
    private List<MatchData> _matchData = new();
    private readonly MLContext _mlContext = new();
    private DataOperationsCatalog.TrainTestData _trainTestData;
    
    public void PrepareDataBy(IEnumerable<MatchData> matchData)
    {
        _matchData = matchData.ToList();
        _dataView = _mlContext.Data.LoadFromEnumerable(_matchData);
        _trainTestData = _mlContext.Data.TrainTestSplit(_dataView, testFraction: 0.2);
    }
    
    public ITransformer TrainModel(PredictionType type)
    {
        var labelColumns = DetermineLabelColumnName(type);
        var featureColumns = DetermineFeatureColumns(type);
        
        var pipeline = _mlContext.Transforms.Conversion
            .MapValueToKey(inputColumnName: labelColumns, outputColumnName: "Label")        
            .Append(_mlContext.Transforms.Categorical
                .OneHotEncoding("HomeEncoded", nameof(MatchData.Home)))
            .Append(_mlContext.Transforms.Categorical
                .OneHotEncoding("AwayEncoded", nameof(MatchData.Away)))
            .Append(_mlContext.Transforms.Concatenate("Features", featureColumns) )
            .Append(_mlContext.BinaryClassification.Trainers.FastForest(labelColumnName: labelColumns));
        
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
        var labelColumns = DetermineLabelColumnName(type);

        var predictions = model.Transform(_trainTestData.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: labelColumns);
        
        return metrics.Accuracy;
    }
    
    public MLPrediction PredictOutcome(MatchData matchData, ITransformer model, PredictionType type)
    {
        var overTwoGoalsPredictionFunction = _mlContext.Model
                .CreatePredictionEngine<MatchData, MLPrediction>(model);
            
        var overTwoGoalsPrediction = overTwoGoalsPredictionFunction.Predict(matchData);

        return overTwoGoalsPrediction;
    }
    
    private static string[] DetermineFeatureColumns(PredictionType type)
    {
        switch (type)
        {
            case PredictionType.OverTwoGoals:
            case PredictionType.UnderThreeGoals:
            case PredictionType.HomeWin:
            case PredictionType.AwayWin:
            case PredictionType.Draw:
            case PredictionType.GoalGoals:
            case PredictionType.TwoToThreeGoals:
                return new[] { 
                    "HomeEncoded", "AwayEncoded", 
                    nameof(MatchData.HomeScoredGoalsAverage),
                    nameof(MatchData.HomeConcededGoalsAverage),
                    nameof(MatchData.HomeHalfTimeScoredGoalsAverage),
                    nameof(MatchData.HomeHalfTimeConcededGoalsAverage),
                    // nameof(MatchData.HomeScoredShotsAverage),
                    // nameof(MatchData.HomeConcededShotsAverage),
                    // nameof(MatchData.HomeScoredTargetShotsAverage),
                    // nameof(MatchData.HomeConcededTargetShotsAverage),
                    
                    nameof(MatchData.AwayScoredGoalsAverage),
                    nameof(MatchData.AwayConcededGoalsAverage),
                    nameof(MatchData.AwayHalfTimeScoredGoalsAverage),
                    nameof(MatchData.AwayHalfTimeConcededGoalsAverage),
                    // nameof(MatchData.AwayScoredShotsAverage),
                    // nameof(MatchData.AwayConcededShotsAverage),
                    // nameof(MatchData.AwayScoredTargetShotsAverage),
                    // nameof(MatchData.AwayConcededTargetShotsAverage)
                };
            default:
                throw new ArgumentException("Invalid prediction type");
        }
    }

    
    private static string DetermineLabelColumnName(PredictionType type)
    {
        return type switch
        {
            PredictionType.OverTwoGoals => nameof(MatchData.OverUnderTwoGoals),
            PredictionType.GoalGoals => nameof(MatchData.BothTeamsScored),
            PredictionType.TwoToThreeGoals => nameof(MatchData.TwoToThreeGoals),
            PredictionType.HomeWin => nameof(MatchData.HomeWin),
            PredictionType.AwayWin => nameof(MatchData.AwayWin),
            _ => throw new ArgumentException("Invalid prediction type")
        };
    }

}
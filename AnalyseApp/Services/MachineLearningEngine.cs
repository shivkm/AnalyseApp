using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class MachineLearningEngine: IMachineLearningEngine
{
    private IDataView _dataView = default!;
    private List<MatchAverage> _matchData = new();
    private readonly MLContext _mlContext = new();
    private ITransformer _model = default!;
    private DataOperationsCatalog.TrainTestData _trainTestData;
    
    public void PrepareDataBy(IEnumerable<MatchAverage> matchData)
    {
        _matchData = matchData.ToList();
        _dataView = _mlContext.Data.LoadFromEnumerable(_matchData);
        _trainTestData = _mlContext.Data.TrainTestSplit(_dataView, testFraction: 0.2);
    }
    
    public ITransformer TrainModel(PredictionType type)
    {
        var labelColumn = DetermineLabelColumnName(type);
        var pipeline =
            _mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: labelColumn)
            .Append(_mlContext.Transforms.Concatenate(
                "Features", 
                nameof(MatchAverage.HomeAverage),
                nameof(MatchAverage.AwayAverage)) 
            )
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: labelColumn,
                numberOfLeaves: 20, 
                numberOfTrees: 100, 
                minimumExampleCountPerLeaf: 10
            ));      
        
        _model = pipeline.Fit(_trainTestData.TrainSet);

        return _model;
    }
    
    public void SaveModel(string modelPath) =>  _mlContext.Model.Save(_model, _dataView.Schema, modelPath);

    public void LoadModel(string modelPath) => _model = _mlContext.Model.Load(modelPath, out _);
    
    public MatchOutcomePrediction PredictOutcome(MatchAverage matchAverage, PredictionType type)
    {
        var overTwoGoalsPredictionFunction = _mlContext.Model
                .CreatePredictionEngine<MatchAverage, MatchOutcomePrediction>(_model);
            
        var output = overTwoGoalsPredictionFunction.Predict(matchAverage);

        return output;
    }
    
    private static string DetermineLabelColumnName(PredictionType type)
    {
        return type switch
        {
            PredictionType.OverTwoGoals => nameof(MatchAverage.OverUnder),
            PredictionType.GoalGoals => nameof(MatchAverage.GoalGoal),
            _ => throw new ArgumentException("Invalid prediction type")
        };
    }
}
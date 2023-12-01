using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class MachineLearning: IMachineLearning
{
    private readonly MLContext _mlContext = new MLContext();

    public MachineLearning()
    {
        
    }
    public IDataView PrepareDataBy(List<Matches> matches)
    {
        // Load your data
        var soccerData = matches
            .Select(m => new SoccerGameData(
                HomeTeam: m.HomeTeam,
                AwayTeam: m.AwayTeam,
                HomeScored: m.FTHG ?? 0,
                AwayScored: m.FTAG ?? 0,
                HomeHalfScored: m.HTHG ?? 0,
                AwayHalfScored: m.HTAG ?? 0,
                IsOverTwoGoals: (m.FTHG ?? 0) + (m.FTAG ?? 0) > 2
                // BothTeamGoals: (m.FTHG ?? 0) > 0 && (m.FTAG ?? 0) > 0,
                // TwoToThreeGoals: ((m.FTHG ?? 0) + (m.FTAG ?? 0) >= 2) && ((m.FTHG ?? 0) + (m.FTAG ?? 0) <= 3),
                // HomeWin: (m.FTHG ?? 0) > (m.FTAG ?? 0),
                // AwayWin: (m.FTHG ?? 0) < (m.FTAG ?? 0)
                ))
            .ToList();
        
        // Load data into IDataView
        var dataView = _mlContext.Data.LoadFromEnumerable(soccerData);

        return dataView; 
    }
    
    
    public (ITransformer transformer, DataOperationsCatalog.TrainTestData trainTestData ) TrainModel(IDataView dataView)
    {
        // Define the training pipeline
        var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding("HomeTeamEncoded", "HomeTeam")
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("AwayTeamEncoded", "AwayTeam"))
            .Append(_mlContext.Transforms.Concatenate("Features", "HomeTeamEncoded", "AwayTeamEncoded", "HomeScored", "AwayScored", "HomeHalfScored", "AwayHalfScored"))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: "IsOverTwoGoals"));

        // Split the data into training and testing datasets
        var splitData = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // Train the model on the training dataset
        var model = pipeline.Fit(dataView);

        return (model, splitData);
    }
    
    public double EvaluateModel(ITransformer model, IDataView testData)
    {
        var predictions = model.Transform(testData);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, "IsOverTwoGoals");

        Console.WriteLine($"Accuracy: {metrics.Accuracy}");
        // Output other metrics as needed

        return metrics.Accuracy;
    }
    
    public bool PredictOutcome(SoccerGameData gameData, ITransformer model)
    {
        var predictionFunction = _mlContext.Model
            .CreatePredictionEngine<SoccerGameData, SoccerGamePrediction>(model);
        
        var prediction = predictionFunction.Predict(gameData);
        return prediction.IsOverTwoGoals;
    }

}
using AnalyseApp.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace AnalyseApp.Services;

public class FastTreePrediction
{
    public void Test()
    {
        // Create a new ML context
        var mlContext = new MLContext();
        var homeTeam = "teamA";
        var awayTeam = "teamB";
        var pastMatches = new List<HistoricalGame>
        {
             new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 2,
                FTAG = 2,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 3,
                FTAG = 1,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 1,
                FTAG = 1,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 2,
                FTAG = 1,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 0,
                FTAG = 0,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 2,
                FTAG = 4,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 4,
                FTAG = 1,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 1,
                FTAG = 2,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 1,
                FTAG = 2,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 3,
                FTAG = 1,
                HTHG = 0,
                HTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 1,
                FTAG = 1,
                HTHG = 0,
                HTAG = 0
            }
        };
        var data = pastMatches.Select(s => new FootballData
        {
            Features = new [] { Convert.ToSingle(s.FTHG), Convert.ToSingle(s.FTAG), Convert.ToSingle(s.HTHG), Convert.ToSingle(s.HTAG) },
            Score = Convert.ToSingle(s.FTHG) + Convert.ToSingle(s.FTAG)
        });
        
        // Load data into an IDataView
        var trainingData = mlContext.Data.LoadFromEnumerable(data);

        // Build the training pipeline
        var pipeline = mlContext.Regression.Trainers.OnlineGradientDescent(labelColumnName: "Score", featureColumnName: "Features");
        
        // Train the model
        var model = pipeline.Fit(trainingData);

        // Use the model to make a prediction
        var predictionEngine = mlContext.Model.CreatePredictionEngine<FootballData, ScorePrediction>(model);

        var gameData = new FootballData { Features = new float[] { 0, 0, 0, 0 } };
        var prediction = predictionEngine.Predict(gameData);
        
        Console.WriteLine("Predicted Score: " + prediction.Score);
    }
}
public class FootballData
{
    [VectorType(4)]
    public float[] Features { get; set; }

    public float Score { get; set; }
}

public class ScorePrediction
{
    public float Score { get; set; }
}
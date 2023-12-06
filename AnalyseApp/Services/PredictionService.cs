using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.Models;
using AnalyseApp.Options;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class PredictionService(
        IFileProcessor fileProcessor,
        IMachineLearningEngine machineLearningEngine, IDataProcessor dataProcessor,
        IOptions<FileProcessorOptions> options): IPredictionService
{
    private readonly string _mlModelPath = options.Value.MachineLearningModel;
    private readonly Dictionary<string, ITransformer> _transformers = new();
    private static readonly string[] LeagueArray = { "E0", "E1", "E2", "F1", "F2", "D1", "D2", "SP1", "SP2", "I1", "I2" };

    public void GenerateFixtureFiles(string fixtureName)
    {
        // Example date ranges for fixture file generation
        var dateRanges = new List<(string startDate, string endDate)>
        {
            ("18/08/23", "21/08/23"),
            ("25/08/23", "28/08/23"),
            ("01/09/23", "04/09/23"),
            ("15/09/23", "18/09/23"),
            ("22/09/23", "25/09/23"),
            ("29/09/23", "02/10/23"),
            ("06/10/23", "09/10/23"),
            ("20/10/23", "23/10/23"),
            ("27/10/23", "30/10/23"),
            ("03/11/23", "06/11/23"),
            ("10/11/23", "13/11/23"),
            ("24/11/23", "27/11/23"),
            ("01/12/23", "04/12/23"),
        };

        foreach (var (startDate, endDate) in dateRanges)
        {
            fileProcessor.CreateFixtureBy(startDate, endDate);
        }
    }
    
    public List<Prediction> GenerateRandomPredictionsBy(int gameCount, string fixture = "fixtures.csv")
    {
        var fixtures = fileProcessor.GetUpcomingGamesBy(fixture)
            .Where(i => LeagueArray.Contains(i.League))
            .OrderByDescending(o => o.Date.Parse())
            .ToList();

        if (fixtures.Count < gameCount)
        {
            Console.WriteLine("Not enough games to generate ticket.");
        }
        
        var predictions = fixtures.Select(ExecutePrediction).ToList();
        var selectedPredictions = SelectRandomPredictions(gameCount, predictions);

        selectedPredictions.ForEach(ii => Console.WriteLine($"{ii.Msg} {ii.Type}"));
        Console.WriteLine("------------------------------------\n");

        return selectedPredictions;
    }
    
    

    private void PrepareDataAndTrainModels(Match upcomingMatch)
    {
        var historicalMatches = fileProcessor
            .GetHistoricalMatchesBy()
            .OrderBy(o => o.Date.Parse())
            .ToList();
        
        var preparedHistoricalData = dataProcessor.CalculateMatchAveragesDataBy(historicalMatches, upcomingMatch);
        machineLearningEngine.PrepareDataBy(preparedHistoricalData);

        foreach (var type in new[] { "Odds", "Goals" }) LoadOrCreateModel(type);
    }

    private void LoadOrCreateModel(string? type)
    {
        var modelFileName = $"{_mlModelPath}/{type}.zip";

        ITransformer bestModel = null;
        var bestAccuracy = 0.0;

        // Load or train and select the best model based on accuracy
        if (File.Exists(modelFileName))
        {
            var loadedModel = machineLearningEngine.LoadModel(modelFileName);
            var accuracy = machineLearningEngine.EvaluateModel(loadedModel, type);

            if (accuracy > bestAccuracy)
            {
                bestModel = loadedModel;
            }
        }
        else
        {
            var trainedModel = machineLearningEngine.TrainModel(type);
            var accuracy = machineLearningEngine.EvaluateModel(trainedModel, type);

            if (accuracy > bestAccuracy)
            {
                bestModel = trainedModel;

                // Save the best model
                machineLearningEngine.SaveModel(bestModel, modelFileName);
            }
        }

        // Set the transformer to the best model
        if (type != null)
        {
            _transformers[type] = bestModel;
        }
    }


    private Prediction ExecutePrediction(Match match)
    {
        PrepareDataAndTrainModels(match);
        var allPredictions = new List<(BetType Type, float Probability)>();

        foreach (var type in Enum.GetValues(typeof(BetType)).Cast<BetType>())
        {
            var predictionTypeName = GetPredictionTypeName(type);
            if (string.IsNullOrEmpty(predictionTypeName)) continue;
            
            var transformer = _transformers[predictionTypeName];
            var probability = machineLearningEngine.EvaluateModel(transformer, predictionTypeName);
            var prediction = machineLearningEngine.PredictOutcome(match, transformer, predictionTypeName);

            
            allPredictions.Add((type, Convert.ToSingle(probability)));
        }

        // Select the prediction with the highest probability
        var bestPrediction = allPredictions.OrderByDescending(p => p.Probability).FirstOrDefault();

        return new Prediction
        {
            Date = match.Date.Parse(),
            HomeScore = match.FullTimeHomeGoals,
            AwayScore = match.FullTimeAwayGoals,
            Msg = $"{match.Date} {match.Time} - {match.HomeTeam}:{match.AwayTeam}",
            Type = bestPrediction.Type,
            Qualified = bestPrediction.Probability > 0
        };
    }
    
    private static string? GetPredictionTypeName(BetType betType)
    {
        return betType switch
        {
            BetType.OverTwoGoals or BetType.GoalGoal => "Goals",
            BetType.HomeWin or BetType.AwayWin => "Odds",
            _ => null
        };
    }
    
    private static List<Prediction> SelectRandomPredictions(int gameCount, List<Prediction> predictions)
    {
        var random = new Random();
        return predictions
            .Where(i => i.Qualified)
            .OrderBy(_ => random.Next())
            .Take(gameCount)
            .ToList();
    }
}


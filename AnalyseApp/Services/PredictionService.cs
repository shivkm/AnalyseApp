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
    
    public List<Prediction> GenerateRandomPredictionsBy(int gameCount, PredictionType type = PredictionType.GoalGoals, string fixture = "fixtures.csv")
    {
        var fixtures = fileProcessor.GetUpcomingGamesBy(fixture)
            .Where(i => LeagueArray.Contains(i.League))
            .OrderByDescending(o => o.Date.Parse())
            .ToList();

        if (fixtures.Count < gameCount)
        {
            Console.WriteLine("Not enough games to generate ticket.");
        }
        
        var predictions = fixtures.Select(i => ExecutePrediction(i, type)).ToList();
        var selectedPredictions = SelectRandomPredictions(gameCount, type, predictions);

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
        TrainAndEvaluateModels();
    }
    
    private void TrainAndEvaluateModels()
    {
        foreach (var type in Enum.GetValues(typeof(PredictionType)).Cast<PredictionType>())
        {
            if (type is PredictionType.Unknown or PredictionType.UnderTwoGoals or PredictionType.Draw or PredictionType.AwayWin)
                continue;

            var modelFileName = $"{_mlModelPath}/{type}.zip";
            ITransformer model;

            if (!File.Exists(modelFileName))
            {
                model = machineLearningEngine.TrainModel(type);
                machineLearningEngine.SaveModel(model, modelFileName);
            }
            else
            {
                model = machineLearningEngine.LoadModel(modelFileName);
            }

            var accuracy = machineLearningEngine.EvaluateModel(model, type);
            Console.WriteLine($"Model for {type}: Accuracy = {accuracy}");

            _transformers[type.ToString()] = model;
        }
    }

    private Prediction ExecutePrediction(Match match, PredictionType ticketType)
    {
        PrepareDataAndTrainModels(match);
        var allPredictions = new List<(PredictionType Prediction, double Probability)>();
        var msg = $"{match.Date} {match.Time} - {match.HomeTeam}:{match.AwayTeam}";
        foreach (var type in Enum.GetValues<PredictionType>())
        {
            if (type is PredictionType.Unknown or PredictionType.UnderTwoGoals or PredictionType.Draw or PredictionType.AwayWin)
                continue;
            
            var transformer = _transformers[type.ToString()];
            var probability = machineLearningEngine.EvaluateModel(transformer, type);
            var prediction = machineLearningEngine.PredictOutcome(match, transformer, type);
            
            msg =  $"{msg} {prediction}, {probability:F}\n";
            allPredictions.Add((prediction, probability));
        }
        
        var bestPrediction = allPredictions
            .FirstOrDefault(item => item.Prediction == ticketType);

        return new Prediction
        {
            Date = match.Date.Parse(),
            HomeScore = match.FullTimeHomeGoals,
            AwayScore = match.FullTimeAwayGoals,
            Msg = msg,
            Type = bestPrediction.Prediction,
            Accuracy = bestPrediction.Probability,
            Qualified = true
        };
    }
    
    
    private static List<Prediction> SelectRandomPredictions(int gameCount, PredictionType type, List<Prediction> predictions)
    {
        var random = new Random();
        return predictions
            .Where(i => i.Qualified && i.Type == type)
            .OrderBy(_ => random.Next())
            .Take(gameCount)
            .ToList();
    }
}


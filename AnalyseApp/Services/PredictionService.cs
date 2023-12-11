using Accord;
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
    private List<MatchData> _mataData = new ();
    private List<Match> _historicalData = new ();
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
            ("05/12/23", "07/12/23"),
            ("08/12/23", "11/12/23"),
        };

        foreach (var (startDate, endDate) in dateRanges)
        {
            fileProcessor.CreateFixtureBy(startDate, endDate);
        }
    }
    
    public List<Prediction> GenerateRandomPredictionsBy(
        int gameCount, 
        PredictionType type = PredictionType.GoalGoals,
        double probability = 0.0,
        string fixture = "fixtures.csv")
    {
        var fixtures = fileProcessor.GetUpcomingGamesBy(fixture)
            .Where(i => LeagueArray.Contains(i.League))
            .OrderByDescending(o => o.Date.Parse())
            .ToList();

        if (fixtures.Count < gameCount)
            Console.WriteLine("Not enough games to generate ticket.");

        var currentDate = fixtures.OrderByDescending(i => i.Date.Parse()).First().Date.Parse();
        PrepareDataAndTrainModels(currentDate);
        var predictions = fixtures.Select(i => ExecutePrediction(i, type)).ToList();
        var selectedPredictions = SelectRandomPredictions(gameCount, type, probability, predictions);

        selectedPredictions.ForEach(ii => Console.WriteLine($"{ii.Msg} \n {ii.Type}"));
        Console.WriteLine("------------------------------------\n");

        return selectedPredictions;
    }

    private void PrepareDataAndTrainModels(DateTime dateTime)
    {
        _historicalData = fileProcessor
            .GetHistoricalMatchesBy()
            .GetHistoricalMatchesOlderThen(dateTime)
            .ToList();
        
        _mataData = dataProcessor.CalculateMatchAveragesDataBy(_historicalData, dateTime);
        machineLearningEngine.PrepareDataBy(_mataData);
        TrainAndEvaluateModels();
    }
    
    private void TrainAndEvaluateModels()
    {
        foreach (var type in Enum.GetValues(typeof(PredictionType)).Cast<PredictionType>())
        {
            if (type is PredictionType.NotQualified or 
                PredictionType.UnderTwoGoals or 
                PredictionType.HomeWin or 
                PredictionType.Draw or 
                PredictionType.AwayWin or 
                PredictionType.Any
                )
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
        var random = new Random();

        var allPredictions = new Dictionary<PredictionType, double>();
        var msg = $"{match.Date} {match.Time} - {match.HomeTeam}:{match.AwayTeam}";
        var homeTeamData = dataProcessor.CalculateTeamData(_historicalData, match.HomeTeam);
        var awayTeamData = dataProcessor.CalculateTeamData(_historicalData, match.AwayTeam);
        var predictMatch = homeTeamData.GetMatchDataBy(awayTeamData, match.Date.Parse());

        if (match.HomeTeam == "Everton")
        {
            
        }
        
        foreach (var type in new[] { PredictionType.OverTwoGoals, PredictionType.GoalGoals, PredictionType.TwoToThreeGoals })
        {
            var transformer = _transformers[type.ToString()];
            var prediction = machineLearningEngine.PredictOutcome(predictMatch, transformer, type);
            
            if (prediction.Prediction is false || prediction.Probability < 0.60)
                continue;
            
            var qualified = prediction.Prediction ? "Qualified" : "Unqualified";
            
            msg = $"{msg}\n {type}: {qualified} probability:{prediction.Probability}";
            allPredictions[type] = prediction.Probability;
        }

        // Determine the highest probability prediction among qualified types
        var bestPrediction = allPredictions
            .OrderByDescending(i => random.Next())
            .FirstOrDefault();
        
        return new Prediction
        {
            Date = match.Date.Parse(),
            HomeScore = match.FullTimeHomeGoals,
            AwayScore = match.FullTimeAwayGoals,
            Msg = msg,
            Type = bestPrediction.Key,
            OverTwoGoalsAccuracy = allPredictions.TryGetValue(PredictionType.OverTwoGoals, out double overTwoProb) ? overTwoProb : 0,
            GoalGoalAccuracy = allPredictions.TryGetValue(PredictionType.GoalGoals, out double goalGoalProb) ? goalGoalProb : 0,
            TwoToThreeGoalsAccuracy = allPredictions.TryGetValue(PredictionType.TwoToThreeGoals, out double twoThreeProb) ? twoThreeProb : 0,
            Qualified = true
        };
    }

    
    private static List<Prediction> SelectRandomPredictions(int gameCount, PredictionType type, double probability, IEnumerable<Prediction> predictions)
    {
        var random = new Random();

        if (type is not PredictionType.Any)
        {
            return predictions
                .Where(i => i.Qualified && i.Type == type)
                .OrderBy(_ => random.Next())
                .Take(gameCount)
                .ToList();
        }
        
        if (probability > 0.0)
        {
            return predictions
                .Where(i => i.Qualified && 
                            (i.GoalGoalAccuracy > probability || i.OverTwoGoalsAccuracy > probability || i.TwoToThreeGoalsAccuracy > probability))
                .OrderBy(_ => random.Next())
                .Take(gameCount)
                .ToList();
        }
        
        if (probability > 0.0 && type is not PredictionType.Any)
        {
            return predictions
                .Where(i => i.Qualified && i.Type == type &&
                            (i.GoalGoalAccuracy > probability || i.OverTwoGoalsAccuracy > probability || i.TwoToThreeGoalsAccuracy > probability))
                .OrderBy(_ => random.Next())
                .Take(gameCount)
                .ToList();
        }
        return predictions
            .Where(i => i.Qualified)
            .OrderBy(_ => random.Next())
            .Take(gameCount)
            .ToList();
    }
}


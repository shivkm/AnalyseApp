using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Options;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class PredictionService(
        IFileProcessor fileProcessor,
        IMachineLearningEngine machineLearningEngine, 
        IOptions<FileProcessorOptions> options): IPredictionService
{
    private readonly string _mlModelPath = options.Value.MachineLearningModel;
    private readonly Dictionary<string, ITransformer> _transformers = new();

    private IDataView _dataView = default!;
    private IDataView _testSet = default!;
    private static readonly string[] LeagueArray = { "E0", "E1", "E2", "F1", "F2", "D1", "D2", "SP1", "SP2", "I1", "I2" };

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

        var playedOn = fixtures.First().Date.Parse();
        PrepareDataAndTrainModels(playedOn);

        var predictions = fixtures.Select(ExecutePrediction).ToList();
        var selectedPredictions = SelectRandomPredictions(gameCount, predictions);

        selectedPredictions.ForEach(ii => Console.WriteLine($"{ii.Msg} {ii.Type}"));
        Console.WriteLine("------------------------------------\n");

        return selectedPredictions;
    }
    
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

    private void PrepareDataAndTrainModels(DateTime date)
    {
        var historicalMatches = fileProcessor.GetHistoricalMatchesBy();
        var historicalData = historicalMatches.OrderMatchesBy(date).ToList();

        _dataView = machineLearningEngine.PrepareDataBy(historicalData);
        var splitData = machineLearningEngine.SplitData(_dataView);
        _testSet = splitData.testSet;

        foreach (var type in new[]
                 {
                     nameof(Match.IsOverTwoGoals),
                     nameof(Match.GoalGoal),
                    // nameof(Match.TwoToThreeGoals),
                     nameof(Match.HomeTeamWin),
                     nameof(Match.AwayTeamWin)
                 })
        {
            LoadOrCreateModel(type, splitData.trainSet);
        }
    }

    private void LoadOrCreateModel(string type, IDataView trainSet)
    {
        var modelFileName = $"{_mlModelPath}/{type}.zip";

        if (!File.Exists(modelFileName))
        {
            var model = machineLearningEngine.TrainModel(trainSet, type);
            machineLearningEngine.SaveModel(model, trainSet, modelFileName);
            _transformers[type] = model;
        }
        else
        {
            _transformers[type] = machineLearningEngine.LoadModel(modelFileName);
        }
    }
    
    private Prediction ExecutePrediction(Match match)
    {
       
        match = GenerateRandomSoccerGameData(match);
        var prediction = new Prediction
        {
            Date = match.Date.Parse(),
            HomeScore = match.FullTimeHomeGoals,
            AwayScore = match.FullTimeAwayGoals,
            Msg = $"{match.Date} {match.Time} - {match.HomeTeam}:{match.AwayTeam}"
        };

        prediction = MakePrediction(match, prediction, nameof(Match.IsOverTwoGoals), BetType.OverTwoGoals);
        if (prediction.Qualified) return prediction;
        
        prediction = MakePrediction(match, prediction, nameof(Match.GoalGoal), BetType.GoalGoal);
        if (prediction.Qualified) return prediction;
        
        // prediction = MakePrediction(match, prediction, nameof(Match.TwoToThreeGoals), BetType.TwoToThreeGoals);
        // if (prediction.Qualified) return prediction;

        prediction = MakePrediction(match, prediction, nameof(Match.HomeTeamWin), BetType.HomeWin);
        if (prediction.Qualified) return prediction;

        prediction = MakePrediction(match, prediction, nameof(Match.AwayTeamWin), BetType.AwayWin);
        return prediction;
    }
    
    private Prediction MakePrediction(Match match, Prediction currentPrediction, string predictionType, BetType betType)
    {
        var transformer = _transformers[predictionType];
        _ = machineLearningEngine.EvaluateModel(transformer, _dataView, predictionType);
        var outcome = machineLearningEngine.PredictOutcome(match, transformer, predictionType);

        if (outcome)
        {
            return currentPrediction with
            {
                Qualified = true,
                Type = betType
            };
        }

        return currentPrediction;
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
    
    private static Match GenerateRandomSoccerGameData(Match match)
    {
        var random = new Random();
    
        if (match.FullTimeHomeGoals != 0 || match.FullTimeAwayGoals != 0)
            return match; // Return the match as-is if goals are already set

        int randomFullTimeHome, randomFullTimeAway;
    
    //    do
    //    {
            randomFullTimeHome = random.Next(0, 4); // Random number between 1 and 3
            randomFullTimeAway = random.Next(0, 4); // Random number between 1 and 3
      //  } 
       // while (randomFullTimeHome + randomFullTimeAway < 3);

        return match with 
        {
            FullTimeHomeGoals = randomFullTimeHome,
            FullTimeAwayGoals = randomFullTimeAway,
            HalfTimeHomeGoals = random.Next(0, 2),
            HalfTimeAwayGoals = random.Next(0, 2)
        };
    }

 
}


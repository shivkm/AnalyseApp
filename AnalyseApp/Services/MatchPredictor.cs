using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Options;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class MatchPredictor(
        IFileProcessor fileProcessor,
        IMachineLearning machineLearning, 
        IOptions<FileProcessorOptions> options): IMatchPredictor
{
    private readonly List<Match> _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
    private readonly string mlModelPath = options.Value.MachineLearningModel;
    private readonly Dictionary<string, ITransformer> _transformers = new();

    private IDataView _dataView;
    private IDataView _testSet;
    private IDataView _trainSet;
    private static readonly string[] sourceArray = { "E0", "E1", "E2", "F1", "F2", "D1", "D2", "SP1", "SP2", "I1", "I2" };

    public List<Prediction> GenerateRandomPredictionsBy(int gameCount, string fixture = "fixtures.csv")
    {
        var fixtures = fileProcessor.GetUpcomingGamesBy(fixture)
            .Where(i => sourceArray.Contains(i.League))
            .ToList();

        if (fixtures.Count < gameCount)
        {
            Console.WriteLine("Not enough games to generate ticket.");
            return null;
        }

        PrepareDataAndTrainModels();

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
        };

        foreach (var (startDate, endDate) in dateRanges)
        {
            fileProcessor.CreateFixtureBy(startDate, endDate);
        }
    }

    public Prediction Execute(Match nextMatch)
    {
        var newMatch = GenerateRandomSoccerGameData(nextMatch);
        var prediction = InitializePrediction(newMatch);

        // Load or train models only once at the beginning of the class lifecycle or on demand
        if (_transformers == null)
        {
            PrepareDataAndTrainModels(); // Ensures all models are ready
        }

        prediction = MakePredictions(newMatch, prediction);

        return prediction;
    }

    private static Prediction InitializePrediction(Match match)
    {
        return new Prediction(BetType.Unknown)
        {
            Date = match.Date.Parse(),
            HomeScore = match.FullTimeHomeGoals,
            AwayScore = match.FullTimeAwayGoals,
            Msg = $"{match.Date} {match.Time} - {match.HomeTeam}:{match.AwayTeam}"
        };
    }

    private Prediction MakePredictions(Match match, Prediction currentPrediction)
    {
        // First, try predicting "Over Two Goals"
        currentPrediction = MakeSinglePrediction(match, currentPrediction, nameof(Match.IsOverTwoGoals), BetType.OverTwoGoals);

        // If "Over Two Goals" is not qualified, then try other predictions
        if (!currentPrediction.Qualified)
        {
            currentPrediction = MakeSinglePrediction(match, currentPrediction, nameof(Match.HomeTeamWin), BetType.HomeWin);
            if (currentPrediction.Qualified) return currentPrediction;

            currentPrediction = MakeSinglePrediction(match, currentPrediction, nameof(Match.AwayTeamWin), BetType.AwayWin);
        }

        return currentPrediction;
    }


    private Prediction MakeSinglePrediction(Match match, Prediction prediction, string predictionType, BetType betType)
    {
        // Load or create the model specific to the prediction type
        LoadOrCreateModel(predictionType, _testSet);
        
        var transformer = _transformers[predictionType];
        var probability = machineLearning.EvaluateModel(transformer, _testSet, predictionType);
        var outcome = machineLearning.PredictOutcome(match, transformer, predictionType);

        if (outcome)
        {
            return prediction with
            {
                Qualified = true,
                Type = betType,
                Percentage = probability,
                Msg = prediction.Msg + betType
            };
        }

        return prediction;
    }

    private void PrepareDataAndTrainModels()
    {
        _dataView = machineLearning.PrepareDataBy(_historicalMatches);
        var splitData = machineLearning.SplitData(_dataView);
        _testSet = splitData.testSet;
        _trainSet = splitData.trainSet;
        
        foreach (var type in new[]
                 {
                     nameof(Match.IsOverTwoGoals),
                     nameof(Match.GoalGoal),
                     nameof(Match.TwoToThreeGoals),
                     nameof(Match.HomeTeamWin),
                     nameof(Match.AwayTeamWin)
                 })
        {
            LoadOrCreateModel(type, splitData.trainSet);
        }
    }

    private void LoadOrCreateModel(string type, IDataView trainSet)
    {
        var modelFileName = $"{mlModelPath}/{type}.zip";

        if (!File.Exists(modelFileName))
        {
            var model = machineLearning.TrainModel(trainSet, type);
            machineLearning.SaveModel(model, trainSet, modelFileName);
            _transformers[type] = model;
        }
        else
        {
            _transformers[type] = machineLearning.LoadModel(modelFileName);
        }
    }
    
    private Prediction ExecutePrediction(Match match)
    {
        var prediction = new Prediction(BetType.Unknown)
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
        
        prediction = MakePrediction(match, prediction, nameof(Match.TwoToThreeGoals), BetType.TwoToThreeGoals);
        if (prediction.Qualified) return prediction;

        prediction = MakePrediction(match, prediction, nameof(Match.HomeTeamWin), BetType.HomeWin);
        if (prediction.Qualified) return prediction;

        prediction = MakePrediction(match, prediction, nameof(Match.AwayTeamWin), BetType.AwayWin);
        return prediction;
    }
    
    private Prediction MakePrediction(Match match, Prediction currentPrediction, string predictionType, BetType betType)
    {
        var transformer = _transformers[predictionType];
        var probability = machineLearning.EvaluateModel(transformer, _dataView, predictionType);
        var outcome = machineLearning.PredictOutcome(match, transformer, predictionType);

        if (outcome)
        {
            return currentPrediction with
            {
                Qualified = true,
                Type = betType,
                Percentage = probability
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
    
        do
        {
            randomFullTimeHome = random.Next(1, 4); // Random number between 1 and 3
            randomFullTimeAway = random.Next(1, 4); // Random number between 1 and 3
        } 
        while (randomFullTimeHome + randomFullTimeAway < 3);

        return match with 
        {
            FullTimeHomeGoals = randomFullTimeHome,
            FullTimeAwayGoals = randomFullTimeAway,
            HalfTimeHomeGoals = random.Next(0, 2),
            HalfTimeAwayGoals = random.Next(0, 2)
        };
    }

 
}


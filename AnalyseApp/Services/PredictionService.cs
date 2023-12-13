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
        DateTime playingOn,
        bool dayPredictions,
        PredictionType type = PredictionType.GoalGoals,
        string fixture = "fixtures.csv")
    {
        var fixtures = fileProcessor.GetUpcomingGamesBy(fixture)
            .Where(i => LeagueArray.Contains(i.League))
            .OrderByDescending(o => o.Date.Parse())
            .ToList();

        
        if (dayPredictions)
        {
            fixtures = fixtures
                .Where(i => i.Date.Parse() == playingOn)
                .ToList();
        }
        
        if (fixtures.Count < gameCount)
            Console.WriteLine("Not enough games to generate ticket.");

        var currentDate = fixtures.OrderByDescending(i => i.Date.Parse()).First().Date.Parse();
        PrepareDataAndTrainModels(currentDate);
        var predictions = fixtures.Select(i => ExecutePrediction(i, type)).ToList();
        var selectedPredictions = SelectRandomPredictions(gameCount, type, predictions);

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
                PredictionType.UnderThreeGoals or
                PredictionType.Draw or 
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

        if (match.HomeTeam == "Braunschweig" || match.HomeTeam == "Sociedad")
        {
            
        }
        
        foreach (var type in new[]
                 {
                     PredictionType.OverTwoGoals, 
                     PredictionType.GoalGoals, 
                     PredictionType.HomeWin, 
                     PredictionType.AwayWin, 
                     PredictionType.TwoToThreeGoals
                 })
        {
            var transformer = _transformers[type.ToString()];
            var prediction = machineLearningEngine.PredictOutcome(predictMatch, transformer, type);
            var poissonProbability = CalculateThePoissonProbabilityBy(predictMatch, type);
            var isUnderThreeGoals = IsZeroZeroOrUnderThreeGoal(predictMatch);
            var gameAverages = GetGameAverageBy(predictMatch, type);

            var finalProbability = (prediction.Probability + gameAverages.average) / 2;
            if ((!prediction.Prediction ||
                 prediction.Prediction && prediction.Probability < 0.60) &&
                type is PredictionType.OverTwoGoals &&
                !gameAverages.qualified && isUnderThreeGoals.qualified)
            {
                msg = $"{msg}\n {PredictionType.UnderThreeGoals}: Qualified probability: {1 - prediction.Probability:F}";
                allPredictions[PredictionType.UnderThreeGoals] = 1 - prediction.Probability;
            }
         
            if (prediction.Prediction &&
                prediction.Probability > 0.55 &&
                type is PredictionType.OverTwoGoals && 
                gameAverages.qualified)
            {
                msg = $"{msg}\n {PredictionType.OverTwoGoals}: Qualified probability: {prediction.Probability}, game averages: {gameAverages.average:F}";
                allPredictions[type] = prediction.Probability;
            }
            if (prediction.Prediction &&
                prediction.Probability > 0.55 &&
                type is PredictionType.GoalGoals)
            {
                msg = $"{msg}\n {PredictionType.GoalGoals}: Qualified probability: {prediction.Probability}, game averages: {gameAverages.average:F}";
                allPredictions[type] = prediction.Probability;
            }
            else
            {
                var qualified = prediction.Prediction ||
                                type is PredictionType.OverTwoGoals &&
                                !isUnderThreeGoals.qualified ? "Qualified" : "Unqualified";
            
                msg = $"{msg}\n {type}: {qualified} probability:{prediction.Probability:F}";
                allPredictions[type] = type is PredictionType.OverTwoGoals && !isUnderThreeGoals.qualified 
                    ? 100 - isUnderThreeGoals.underThreeGoals : prediction.Probability;
            }
        }

        var bestPrediction = allPredictions
            .OrderByDescending(i => i.Value)
            .FirstOrDefault();

        return new Prediction
        {
            Date = match.Date.Parse(),
            HomeScore = match.FullTimeHomeGoals,
            AwayScore = match.FullTimeAwayGoals,
            Msg = msg,
            Type = bestPrediction.Key,
            OverTwoGoalsAccuracy = allPredictions.TryGetValue(PredictionType.OverTwoGoals, out var overTwoProb) ? overTwoProb : 0,
            GoalGoalAccuracy = allPredictions.TryGetValue(PredictionType.GoalGoals, out var goalGoalProb) ? goalGoalProb : 0,
            TwoToThreeGoalsAccuracy = allPredictions.TryGetValue(PredictionType.TwoToThreeGoals, out var twoThreeProb) ? twoThreeProb : 0,
            Qualified = true,
            Probability = bestPrediction.Value
        };
    }


    private static (bool qualified, double underThreeGoals, double zeroZero) IsZeroZeroOrUnderThreeGoal(MatchData matchData)
    {
        var zeroZeroMatches = matchData.HomeZeroZeroMatchAverage + matchData.AwayZeroZeroMatchAverage;
        var underThreeGoalsMatches =
            (matchData.HomeUnderThreeGoalsMatchAverage + matchData.AwayUnderThreeGoalsMatchAverage) / 2;

        var qualified = zeroZeroMatches > 0.20 || underThreeGoalsMatches > 0.50;

        return (qualified, underThreeGoalsMatches, zeroZeroMatches);
    }
    
    private static (bool qualified, double average) GetGameAverageBy(MatchData matchData, PredictionType type)
    {
        var probability = matchData.HomeZeroZeroMatchAverage + matchData.AwayZeroZeroMatchAverage;
        var qualified = probability > 0.20;

        switch (type)
        {
            case PredictionType.GoalGoals:
                probability = (matchData.HomeGoalGoalMatchAverage + matchData.AwayGoalGoalMatchAverage) / 2;
                qualified = probability > 0.60;
                break;
            case PredictionType.UnderThreeGoals:
                probability = (matchData.HomeUnderThreeGoalsMatchAverage + matchData.AwayUnderThreeGoalsMatchAverage) / 2;
                qualified = probability > 0.55;
                break;
            case PredictionType.OverTwoGoals:
                probability = (matchData.HomeOverTwoGoalsMatchAverage + matchData.AwayOverTwoGoalsMatchAverage) / 2;
                qualified = probability > 0.60;
                break;
        }

        return (qualified, probability);
    }
    
    private static double CalculateThePoissonProbabilityBy(MatchData matchData, PredictionType type)
    {
        var expectedGoals = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    
        // Corrected goal expectation calculations
        var homeAverage = matchData.HomeScoredGoalsAverage / matchData.AwayConcededGoalsAverage;
        var awayAverage = matchData.AwayScoredGoalsAverage / matchData.HomeConcededGoalsAverage;

    
        var overTwoGoalProbabilities = new List<double>();
        var goalGoalProbabilities = new List<double>();
        var twoToThreeGoalProbabilities = new List<double>();
    
        foreach (var homeExpectedGoal in expectedGoals)
        {
            foreach (var awayExpectedGoal in expectedGoals)
            {
                var homeTeamProbability = homeAverage.PoissonProbability(homeExpectedGoal);
                var awayTeamProbability = awayAverage.PoissonProbability(awayExpectedGoal);
            
                // Corrected condition for over two goals
                if (homeExpectedGoal + awayExpectedGoal > 2)
                {
                    overTwoGoalProbabilities.Add(homeTeamProbability * awayTeamProbability);
                }
            
                // Corrected condition for goal-goal (both teams score)
                if (homeExpectedGoal > 0 && awayExpectedGoal > 0)
                {
                    goalGoalProbabilities.Add(homeTeamProbability * awayTeamProbability);
                }
            
                // Corrected condition for two to three goals in total
                if (homeExpectedGoal + awayExpectedGoal is 2 or 3)
                {
                    twoToThreeGoalProbabilities.Add(homeTeamProbability * awayTeamProbability);
                }
            }
        }

        // Return the mean probability based on the prediction type
        return type switch
        {
            PredictionType.GoalGoals => goalGoalProbabilities.DefaultIfEmpty(0).Average(),
            PredictionType.OverTwoGoals => overTwoGoalProbabilities.DefaultIfEmpty(0).Average(),
            PredictionType.TwoToThreeGoals => twoToThreeGoalProbabilities.DefaultIfEmpty(0).Average(),
            _ => 0.0
        };
        
    }
    
    private static List<Prediction> SelectRandomPredictions(int gameCount, PredictionType type, IEnumerable<Prediction> predictions)
    {
        var random = new Random();
        var selectedTickets = new List<Prediction>();

        var goalGoalGames = predictions
            .Where(i => i is { Qualified: true, Type: PredictionType.GoalGoals })
            .OrderBy(_ => random.Next())
            .Take(2)
            .ToList();
        selectedTickets.AddRange(goalGoalGames);
        
        var overTwoGoalsGames = predictions
            .Where(i => i is { Qualified: true, Type: PredictionType.OverTwoGoals })
            .OrderBy(_ => random.Next())
            .Take(2)
            .ToList();
        selectedTickets.AddRange(overTwoGoalsGames);
        
        var underThreeGoalsGames = predictions
            .Where(i => i is { Qualified: true, Type: PredictionType.UnderThreeGoals })
            .OrderBy(_ => random.Next())
            .Take(2)
            .ToList();    
        selectedTickets.AddRange(underThreeGoalsGames);
        
        // var twoToThreeGoalsGames = predictions
        //     .Where(i => i is { Qualified: true, Type: PredictionType.TwoToThreeGoals })
        //     .OrderBy(_ => random.Next())
        //     .Take(2)
        //     .ToList();  
        // selectedTickets.AddRange(twoToThreeGoalsGames);
        //
        // var homeWinGames = predictions
        //     .Where(i => i is { Qualified: true, Type: PredictionType.HomeWin })
        //     .OrderBy(_ => random.Next())
        //     .Take(2)
        //     .ToList();
        // selectedTickets.AddRange(homeWinGames);
        //
        // var awayWinGames = predictions
        //     .Where(i => i is { Qualified: true, Type: PredictionType.AwayWin })
        //     .OrderBy(_ => random.Next())
        //     .Take(2)
        //     .ToList();
        // selectedTickets.AddRange(awayWinGames);
        
        return selectedTickets
            .OrderBy(i => random.Next())
            .Take(gameCount)
            .ToList();
    }
}


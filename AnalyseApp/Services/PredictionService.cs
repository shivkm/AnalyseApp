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
    private static readonly string[] LeagueArray = { "E0", "E1", "E2", "SP1", };

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
    
    public List<Prediction> GenerateRandomPredictionsBy(
        int gameCount, 
        PredictionType type = PredictionType.GoalGoals,
        double probability = 0.0,
        string fixture = "fixtures.csv")
    {
        var fixtures = fileProcessor.GetUpcomingGamesBy(fixture)
            .Where(i => LeagueArray.Contains(i.League))
            .OrderByDescending(o => o.Date.Parse())
            .Take(20)
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
        // Iterate through relevant prediction types
        foreach (var type in new[] { PredictionType.OverTwoGoals, PredictionType.GoalGoals, PredictionType.TwoToThreeGoals })
        {
            var transformer = _transformers[type.ToString()];
            var probability = machineLearningEngine.EvaluateModel(transformer, type);
            var prediction = machineLearningEngine.PredictOutcome(predictMatch, transformer, type);
            
            if (prediction == PredictionType.NotQualified || probability < 0.58)
                continue;

            var expectedHomeGoal = type == PredictionType.OverTwoGoals ? 2 : 1;
            var expectedAwayGoal = type == PredictionType.OverTwoGoals ? 2 : 1;
            
            var homeGoalExpectation = Convert.ToDouble(homeTeamData.ScoredGoalsAverage) * 
                                            Convert.ToDouble(awayTeamData.ConcededGoalsAverage);
            var awayGoalExpectation = Convert.ToDouble(awayTeamData.ScoredGoalsAverage) *
                                            Convert.ToDouble(homeTeamData.ConcededGoalsAverage);

            var homeTeamProbability = homeGoalExpectation.PoissonProbability(expectedHomeGoal);
            var awayTeamProbability = awayGoalExpectation.PoissonProbability(expectedAwayGoal);

            var Poissonprobability = (homeTeamProbability + awayTeamProbability);

            msg = $"{msg}\n {type}: {prediction} probability:{probability} Poisson Score Probability: {Poissonprobability}";
            allPredictions[type] = probability;
        }

        // Determine the highest probability prediction among qualified types
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
            OverTwoGoalsAccuracy = allPredictions.TryGetValue(PredictionType.OverTwoGoals, out double overTwoProb) ? overTwoProb : 0,
            GoalGoalAccuracy = allPredictions.TryGetValue(PredictionType.GoalGoals, out double goalGoalProb) ? goalGoalProb : 0,
            TwoToThreeGoalsAccuracy = allPredictions.TryGetValue(PredictionType.TwoToThreeGoals, out double twoThreeProb) ? twoThreeProb : 0,
            Qualified = bestPrediction.Value > 0.58
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


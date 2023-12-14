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
        var matchAverage = dataProcessor.CalculateGoalMatchAverageBy(_historicalData, match.HomeTeam, match.AwayTeam);
        var poissonProbability = CalculateThePoissonProbabilityBy(matchAverage);
        
        if (match.HomeTeam == "Kaiserslautern" || match.HomeTeam == "Valencia")
        {
            
        }

        var testMsg = string.Empty;
        
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
            // var isUnderThreeGoals = IsZeroZeroOrUnderThreeGoal(predictMatch);
            var gameAverages = GetGameAverageBy(predictMatch, type);

            // if (poissonProbability.Type == type && gameAverages.qualified)
            // {
            //     msg = $"{msg}\n {type}: Qualified probability: {poissonProbability.Probability:F}";
            //     allPredictions[type] = poissonProbability.Probability;
            // }
            // if ((poissonProbability.Type == type ||
            //      poissonProbability.Type is PredictionType.UnderThreeGoals && type is PredictionType.TwoToThreeGoals) &&
            //     gameAverages.qualified)
            // {
            //     msg = $"{msg}\n {type}: Qualified probability: {poissonProbability.Probability:F}";
            //     allPredictions[type] = poissonProbability.Probability;
            // }

            var qualified = prediction.Prediction;
            msg = $"{msg}\n {type}: {qualified}, probability: {poissonProbability.Probability:F}";
            allPredictions[type] = poissonProbability.Probability;

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
            Qualified = bestPrediction.Value > 0.50,
            Probability = bestPrediction.Value
        };
    }


    private static (bool qualified, double underThreeGoals, double zeroZero) IsZeroZeroOrUnderThreeGoal(MatchData matchData)
    {
        var zeroZeroMatches = matchData.HomeZeroZeroMatchAverage + matchData.AwayZeroZeroMatchAverage;
        var underThreeGoalsMatches =
            (matchData.HomeUnderThreeGoalsMatchAverage + matchData.AwayUnderThreeGoalsMatchAverage) / 2;

        var qualified = zeroZeroMatches > 0.25 || underThreeGoalsMatches > 0.58;

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
            case PredictionType.TwoToThreeGoals:
                probability = (matchData.HomeTwoToThreeGoalsMatchAverage + matchData.AwayTwoToThreeGoalsMatchAverage) / 2;
                qualified = probability >= 0.50;
                break;
        }

        return (qualified, probability);
    }
    
    private static readonly int[] ExpectedGoals = { 0, 1, 2, 3, 4, 5 };

    private static PoissonProbability CalculateThePoissonProbabilityBy(MatchAverage matchAverage)
    {
        var matchOutcomes = CalculateMatchOutcomes(matchAverage);
        var highestProbability = FindHighestProbability(matchOutcomes);

        return highestProbability;
    }

    private static Dictionary<PredictionType, double> CalculateMatchOutcomes(MatchAverage matchAverage)
    {
        var matchOutcomes = new Dictionary<PredictionType, double>
        {
            { PredictionType.HomeWin, 0 },
            { PredictionType.AwayWin, 0 },
            { PredictionType.Draw, 0 },
            { PredictionType.OverTwoGoals, 0 },
            { PredictionType.GoalGoals, 0 },
            { PredictionType.TwoToThreeGoals, 0 },
            { PredictionType.UnderThreeGoals, 0 }
        };

        foreach (var homeGoals in ExpectedGoals)
        {
            foreach (var awayGoals in ExpectedGoals)
            {
                var homeProbability = matchAverage.HomeAverage.PoissonProbability(homeGoals);
                var awayProbability = matchAverage.AwayAverage.PoissonProbability(awayGoals);
                var combinedProbability = homeProbability * awayProbability;

                UpdateProbabilities(matchOutcomes, homeGoals, awayGoals, combinedProbability);            }
        }
        
        return matchOutcomes;
    }

    private static void UpdateProbabilities(Dictionary<PredictionType, double> matchOutcomes, int homeGoals, int awayGoals, double probability)
    {
        if (homeGoals > awayGoals)
            matchOutcomes[PredictionType.HomeWin] += probability;
        else if (homeGoals < awayGoals)
            matchOutcomes[PredictionType.AwayWin] += probability;
        else
            matchOutcomes[PredictionType.Draw] += probability;

        if (homeGoals + awayGoals > 2)
            matchOutcomes[PredictionType.OverTwoGoals] += probability;
    
        if (homeGoals > 0 && awayGoals > 0)
            matchOutcomes[PredictionType.GoalGoals] += probability;
    
        if (homeGoals + awayGoals is 2 or 3)
            matchOutcomes[PredictionType.TwoToThreeGoals] += probability;
    
        if (homeGoals + awayGoals < 3)
            matchOutcomes[PredictionType.UnderThreeGoals] += probability;
    }

    private static PoissonProbability FindHighestProbability(Dictionary<PredictionType, double> matchOutcomes)
    {
        var highest = matchOutcomes.MaxBy(kvp => kvp.Value);
        return new PoissonProbability(highest.Key, highest.Value);
    }

    private static Dictionary<PredictionType, PoissonProbability> SummarizeProbabilities(Dictionary<PredictionType, List<double>> matchOutcomes)
    {
        return matchOutcomes.ToDictionary(
            outcome => outcome.Key,
            outcome => new PoissonProbability(outcome.Key, outcome.Value.Sum()));
    }

    private static List<Prediction> SelectRandomPredictions(int gameCount, PredictionType type, IEnumerable<Prediction> predictions)
    {
        var random = new Random();
        var selectedTickets = new List<Prediction>();

        var goalGoalGames = predictions
            .Where(i => i is { Qualified: true, Type: PredictionType.GoalGoals })
            .OrderBy(i => i.Probability)
            .Take(gameCount)
            .ToList();
        selectedTickets.AddRange(goalGoalGames);
        
        var overTwoGoalsGames = predictions
            .Where(i => i is { Qualified: true, Type: PredictionType.OverTwoGoals })
            .OrderBy(i => i.Probability)
            .Take(gameCount)
            .ToList();
        selectedTickets.AddRange(overTwoGoalsGames);
        
        // var underThreeGoalsGames = predictions
        //     .Where(i => i is { Qualified: true, Type: PredictionType.UnderThreeGoals })
        //     .OrderBy(_ => random.Next())
        //     .Take(2)
        //     .ToList();    
        // selectedTickets.AddRange(underThreeGoalsGames);
        
        // var twoToThreeGoalsGames = predictions
        //     .Where(i => i is { Qualified: true, Type: PredictionType.TwoToThreeGoals })
        //     .OrderBy(_ => random.Next())
        //     .Take(2)
        //     .ToList();  
        // selectedTickets.AddRange(twoToThreeGoalsGames);
        
        var homeWinGames = predictions
            .Where(i => i is { Qualified: true, Type: PredictionType.HomeWin })
            .OrderBy(i => i.Probability)
            .Take(gameCount)
            .ToList();
        selectedTickets.AddRange(homeWinGames);
        
        var awayWinGames = predictions
            .Where(i => i is { Qualified: true, Type: PredictionType.AwayWin })
            .OrderBy(i => i.Probability)
            .Take(gameCount)
            .ToList();
        selectedTickets.AddRange(awayWinGames);
        
        return predictions
            .OrderBy(i => i.Probability)
            .Take(gameCount)
            .ToList();
    }
}


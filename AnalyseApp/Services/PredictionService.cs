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
    private List<MatchAverage> _mataData = new ();
    private List<Match> _historicalData = new ();
    //private static readonly string[] LeagueArray = { "E0", "E1", "E2", "F1", "F2", "D1", "D2", "SP1", "SP2", "I1", "I2" };
    private static readonly string[] LeagueArray = { "E0", "E1", "E2", "F1", "D1", "SP1", "I1" };
    
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
        foreach (var type in new[] { PredictionType.OverTwoGoals,  PredictionType.GoalGoals })
        {
            var modelFileName = $"{_mlModelPath}/{type}.zip";
            ITransformer model;

            if (!File.Exists(modelFileName))
            {
                machineLearningEngine.TrainModel(type);
                machineLearningEngine.SaveModel(modelFileName);
            }
            else
            {
                machineLearningEngine.LoadModel(modelFileName);
            }
        }
    }

    private Prediction ExecutePrediction(Match match, PredictionType ticketType)
    {
        var allPredictions = new List<PoissonProbability>();
        var msg = $"{match.Date} {match.Time} - {match.HomeTeam}:{match.AwayTeam}";
        var matchAverage = dataProcessor.CalculateGoalMatchAverageBy(
            _historicalData,
            match.HomeTeam, 
            match.AwayTeam,
            match.Date.Parse(),
            0, 
            0);
        var homeTeamData = dataProcessor.CalculateTeamData(_historicalData, match.HomeTeam);
        var awayTeamData = dataProcessor.CalculateTeamData(_historicalData, match.AwayTeam);
        var predictMatch = homeTeamData.GetMatchDataBy(awayTeamData, match.Date.Parse());
        
        var poissonProbability = CalculateThePoissonProbabilityBy(matchAverage);
        var matchAnalysis = dataProcessor.GetLastSixMatchDataBy(
            _historicalData,
            match.HomeTeam,
            match.AwayTeam,
            match.Date.Parse()
        );
       
        if (match.HomeTeam is "Portsmouth" or "Valencia")
        {
            
        }

        foreach (var type in new[] { PredictionType.OverTwoGoals, PredictionType.GoalGoals })
        {
            
            var prediction = machineLearningEngine.PredictOutcome(matchAverage, type);
            
            if (
                (matchAnalysis.HomeTeam.LastMatchNotScored && matchAnalysis.AwayTeam.LastMatchNotScored)
            )
            {
                if (prediction.PredictedLabel && prediction.Probability > 0.60)
                {
                    allPredictions.Add(new PoissonProbability(type, prediction.Probability));
                    msg = $"{msg}\n {type}: Qualified probability: {100 * prediction.Probability:F}";
                }

                if (type is PredictionType.GoalGoals)
                {
                    msg = $"{msg}\n {type}: Qualified";
                }
            }

            
            
            
           

            // if (!prediction.PredictedLabel && type is PredictionType.OverTwoGoals)
            // {
            //     allPredictions.Add(new PoissonProbability(PredictionType.UnderThreeGoals, 1 - prediction.Probability));
            //     msg = $"{msg}\n {PredictionType.UnderThreeGoals}: Qualified probability: {1 - prediction.Probability:F}";
            // }

        }

        var bestPrediction = allPredictions
                                 .FirstOrDefault(i => i.Type == poissonProbability.Type) 
                             ?? allPredictions.MaxBy(i => i.Probability);
        
        if (bestPrediction is null)
        {
            return new Prediction();
        }

        var qualifiedForOver = allPredictions
            .Any(item => item.Type is PredictionType.OverTwoGoals);
        
        var qualifiedForGoalGoal = allPredictions
            .Any(item => item.Type is PredictionType.GoalGoals);
            
        if (poissonProbability.Type is PredictionType.UnderThreeGoals &&
            poissonProbability.Probability > 0.50 && qualifiedForOver && qualifiedForGoalGoal)
        {
            bestPrediction = allPredictions.FirstOrDefault(item => item.Type is PredictionType.GoalGoals);
        }
        if (bestPrediction.Type is PredictionType.UnderThreeGoals && poissonProbability.Probability > 0.70)
        {
            bestPrediction = poissonProbability;
        }
       
        
        msg = $"{msg}\n poisson suggestion: {poissonProbability.Type}: Qualified, Probability {100 * poissonProbability.Probability:F}%\n" +
              $"Last Six Match Analysis: Home: {matchAnalysis.HomeTeam.TeamName},\n " +
              $"ScoredGoalInLastThreeMatches: {matchAnalysis.HomeTeam.Output},\n  " +
              $"Away: {matchAnalysis.AwayTeam.TeamName},\n " +
              $"ScoredGoalInLastThreeMatches: {matchAnalysis.AwayTeam.Output},\n " +
              $"Head to Head: Home Scored Goal {matchAnalysis.HeadToHeadData.ScoredHomeGoal}, Away Scored Goal {matchAnalysis.HeadToHeadData.ScoredAwayGoal} out of {matchAnalysis.HeadToHeadData.Count}"
            ;
        
        return new Prediction
        {
            Date = match.Date.Parse(),
            HomeScore = match.FullTimeHomeGoals,
            AwayScore = match.FullTimeAwayGoals,
            Msg = msg,
            Type = bestPrediction.Type,
            Qualified = bestPrediction.Probability > 0.60,
            Probability = bestPrediction.Probability
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
    
    private static PoissonProbability GetGameAverageBy(MatchData matchData, PredictionType type)
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

        return new PoissonProbability(type, probability);
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
        // var selectedTickets = new List<Prediction>();
        //
        // var goalGoalGames = predictions
        //     .Where(i => i is { Qualified: true, Type: PredictionType.GoalGoals })
        //     .OrderBy(i => i.Probability)
        //     .Take(gameCount)
        //     .ToList();
        // selectedTickets.AddRange(goalGoalGames);
        //
        // var overTwoGoalsGames = predictions
        //     .Where(i => i is { Qualified: true, Type: PredictionType.OverTwoGoals })
        //     .OrderBy(i => i.Probability)
        //     .Take(gameCount)
        //     .ToList();
        // selectedTickets.AddRange(overTwoGoalsGames);
        //
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
        //
        // var homeWinGames = predictions
        //     .Where(i => i is { Qualified: true, Type: PredictionType.HomeWin })
        //     .OrderBy(i => i.Probability)
        //     .Take(gameCount)
        //     .ToList();
        // selectedTickets.AddRange(homeWinGames);
        //
        // var awayWinGames = predictions
        //     .Where(i => i is { Qualified: true, Type: PredictionType.AwayWin })
        //     .OrderBy(i => i.Probability)
        //     .Take(gameCount)
        //     .ToList();
        // selectedTickets.AddRange(awayWinGames);
        //
        if (type is PredictionType.Any)
        {
            return predictions.Where(i => i.Qualified)
                .OrderBy(_ => Random.Shared.Next())
                .Take(gameCount)
                .ToList();
        }
        return predictions.Where(i => i.Qualified)
            .OrderBy(i => i.Probability)
            .Take(gameCount)
            .ToList();
    }
}


using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class MatchPredictor: IMatchPredictor
{   
    private readonly List<Match> _historicalMatches;
    private readonly IMachineLearning _machineLearning;
    private readonly IFileProcessor _fileProcessor;
    private ITransformer transformer;
    private DataOperationsCatalog.TrainTestData trainTestData;
    
    public MatchPredictor(IFileProcessor fileProcessor, IMachineLearning machineLearning)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
        _fileProcessor = fileProcessor;
        _machineLearning = machineLearning;
    }

    public List<Ticket>? GenerateTicketBy(int gameCount, int ticketCount, BetType type, string fixture = "fixtures.csv")
    {
        var fixtures = _fileProcessor.GetUpcomingGamesBy(fixture);
        var predictions = new List<Prediction>();

        fixtures = fixtures.Where(i =>
            i.Div == "E0" || 
            i.Div == "E1" || 
            i.Div == "E2" || 
            i.Div == "F1" || 
            i.Div == "F2" || 
            i.Div == "D1" || 
            i.Div == "D2" || 
            i.Div == "SP1" || 
            i.Div == "SP2" || 
            i.Div == "I1" || 
            i.Div == "I2" 
            ).ToList();
        foreach (var nextMatch in fixtures)
        {
            var prediction = Execute(nextMatch);
            predictions.Add(prediction);
        }
        
        // Ensure there are enough games for the random selection
        if (predictions.Count < gameCount)
        {
            Console.WriteLine("Not enough games to generate ticket.");
            return null;
        }

        var finalPredictions = GenerateTicketWithRandomMatches(gameCount, predictions);
        finalPredictions.ForEach(ii => { Console.WriteLine($"{ii.Msg}"); });
        Console.WriteLine("------------------------------------\\n");
        // var tickets = new List<Ticket>();
        // for (var i = 0; i < ticketCount; i++)
        // {
        //     var finalPredictions = GenerateTicketWithRandomMatches(gameCount, predictions);
        //     tickets.Add(new Ticket(finalPredictions));
        // }
        //
        // tickets.ForEach(i => { i.Predictions.ForEach(ii => { Console.WriteLine($"{ii.Msg}\n"); }); });
        return null;
    }
    

    private static List<Prediction> GenerateTicketWithRandomMatches(int gameCount, List<Prediction> predictions)
    {
        // Randomly select games based on gameCount
        var random = new Random();

        var selectedPredictions = predictions
            .Where(i => i.Qualified)
            .OrderBy(_ => random.Next())
            .Take(gameCount)
            .ToList();

        // Output prediction messages
        var finalPredictions = new List<Prediction>();
        foreach (var selectedPrediction in selectedPredictions)
        {
            var msg = $"{selectedPrediction.Msg} {selectedPrediction.Type}";
            finalPredictions.Add(selectedPrediction with { Msg = msg });
        }

        return finalPredictions;
    }
    

    public void GenerateFixtureFiles(string fixtureName)
    {
        _fileProcessor.CreateFixtureBy("18/08/23", "21/08/23");
        _fileProcessor.CreateFixtureBy("25/08/23", "28/08/23");
        _fileProcessor.CreateFixtureBy("01/09/23", "04/09/23");
        _fileProcessor.CreateFixtureBy("15/09/23", "18/09/23");
        _fileProcessor.CreateFixtureBy("22/09/23", "25/09/23");
        _fileProcessor.CreateFixtureBy("29/09/23", "02/10/23");
        _fileProcessor.CreateFixtureBy("06/10/23", "09/10/23");
        _fileProcessor.CreateFixtureBy("20/10/23", "23/10/23");
        _fileProcessor.CreateFixtureBy("27/10/23", "30/10/23");
        _fileProcessor.CreateFixtureBy("03/11/23", "06/11/23");
        _fileProcessor.CreateFixtureBy("10/11/23", "13/11/23");
        _fileProcessor.CreateFixtureBy("24/11/23", "27/11/23");
    }

    public Prediction Execute(Match nextMatch)
    {
        var soccerGameData = GenerateRandomSoccerGameData(nextMatch);
        var prediction = new Prediction(BetType.Unknown)
        {
            HomeScore = nextMatch.FTHG,
            AwayScore = nextMatch.FTAG,
            Msg = $"{nextMatch.Date} {nextMatch.Time} - {nextMatch.HomeTeam}:{nextMatch.AwayTeam}"
        };

        // Prepare data
        var dataView = _machineLearning.PrepareDataBy(_historicalMatches);
        
        // Train the model and Split the data into training and testing datasets
        var model = _machineLearning.TrainModel(dataView);
        transformer = model.transformer;
        trainTestData = model.trainTestData;
        
        prediction = PredictionGoalGoal(soccerGameData, prediction);
        if (prediction.Qualified) return prediction;

        prediction = PredictionOverTwoGoal(soccerGameData, prediction);
        if (prediction.Qualified) return prediction;

        prediction = PredictionHomeWin(soccerGameData, prediction);
        if (prediction.Qualified) return prediction;

        prediction = PredictionAwayWin(soccerGameData, prediction);
        if (prediction.Qualified) return prediction;

        return prediction;
    }
    
    private Prediction PredictionGoalGoal(SoccerGameData newGameData, Prediction prediction)
    {
        // Evaluate the model
        var goalGoalProbability = _machineLearning.EvaluateModel(
            transformer,
            trainTestData.TestSet,
            nameof(SoccerGameData.BothTeamGoals)
        );

        var goalGoal = _machineLearning.PredictOutcome(newGameData, transformer, nameof(SoccerGameData.BothTeamGoals));
        if (goalGoal)
        {
            return prediction with
            {
                Qualified = true,
                Type = BetType.GoalGoal,
                Percentage = goalGoalProbability
            };
        }

        return prediction;
    }

    private Prediction PredictionOverTwoGoal(SoccerGameData newGameData, Prediction prediction)
    {
        // Evaluate the model
        var overTwoGoalProbability = _machineLearning.EvaluateModel(
            transformer,
            trainTestData.TestSet,
            nameof(SoccerGameData.IsOverTwoGoals)
        );

        var overTwoGoals = _machineLearning.PredictOutcome(newGameData, transformer, nameof(SoccerGameData.IsOverTwoGoals));
        return overTwoGoals switch
        {
            true => prediction with
            {
                Qualified = true, Type = BetType.OverTwoGoals, Percentage = overTwoGoalProbability
            },
            false => prediction with
            {
                Qualified = true, Type = BetType.UnderThreeGoals, Percentage = overTwoGoalProbability
            }
        };
    }

    private Prediction PredictionHomeWin(SoccerGameData newGameData, Prediction prediction)
    {
        // Evaluate the model
        var homeWinProbability = _machineLearning.EvaluateModel(
            transformer,
            trainTestData.TestSet,
            nameof(SoccerGameData.HomeTeamWin)
        );

        var homeWin = _machineLearning.PredictOutcome(newGameData, transformer, nameof(SoccerGameData.HomeTeamWin));
        if (homeWin)
        {
            return prediction with
            {
                Qualified = true,
                Type = BetType.HomeWin,
                Percentage = homeWinProbability
            };
        }

        return prediction;
    }
    
    private Prediction PredictionAwayWin(SoccerGameData newGameData, Prediction prediction)
    {
        // Evaluate the model
        var awayWinProbability = _machineLearning.EvaluateModel(
            transformer,
            trainTestData.TestSet,
            nameof(SoccerGameData.AwayTeamWin)
        );

        var awayWin = _machineLearning.PredictOutcome(newGameData, transformer, nameof(SoccerGameData.AwayTeamWin));
        if (awayWin)
        {
            return prediction with
            {
                Qualified = true,
                Type = BetType.AwayWin,
                Percentage = awayWinProbability
            };
        }

        return prediction;
    }
    
    private static SoccerGameData GenerateRandomSoccerGameData(Match match)
    {
        var random = new Random();
        var randomFullTimeHome = 0;
        var randomFullTimeAway = 0;
        var randomHomeHalfScored = random.Next(0, 2);
        var randomAwayHalfScored = random.Next(0, 2);

        // Generating random numbers for FullTimeHome and FullTimeAway such that their sum is always more than 2
        do
        {
            randomFullTimeHome = random.Next(1, 4); // Random number between 1 and 3
            randomFullTimeAway = random.Next(1, 4); // Random number between 1 and 3
        }
        while (randomFullTimeHome + randomFullTimeAway > 2);
        
        var newGameData = new SoccerGameData(
            match.HomeTeam, 
            match.AwayTeam, 
            randomFullTimeHome, 
            randomFullTimeAway, 
            randomHomeHalfScored, 
            randomAwayHalfScored, 
            randomFullTimeHome + randomFullTimeAway > 2, 
            randomFullTimeHome > 0 && randomFullTimeAway > 0,
            randomFullTimeHome > randomFullTimeAway,
            randomFullTimeHome < randomFullTimeAway
        );
        
        if (match is { FTHG: not null, FTAG: not null })
        {
            newGameData = new SoccerGameData(
                match.HomeTeam, 
                match.AwayTeam, 
                match.FTHG.Value, 
                match.FTAG.Value, 
                match.HTHG.Value,
                match.HTAG.Value, 
                match.FTHG.Value + match.FTAG.Value > 2,
                match.FTHG.Value > 0 && match.FTAG.Value > 0,
                match.FTHG > match.FTAG,
                match.FTAG > match.FTHG);
        }
        
        return newGameData;
    }
}


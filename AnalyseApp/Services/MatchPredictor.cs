using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class MatchPredictor: IMatchPredictor
{   
    private readonly List<Matches> _historicalMatches;
    private readonly IDataService _dataService;
    private readonly IFileProcessor _fileProcessor;
    
    public MatchPredictor(IFileProcessor fileProcessor, IDataService dataService)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
        _dataService = dataService;
        _fileProcessor = fileProcessor;
    }

    public List<Ticket>? GenerateTicketBy(int gameCount, int ticketCount, BetType type, string fixture = "fixture-24-11")
    {
        var fixtures = _fileProcessor.GetUpcomingGamesBy(fixture);
        var predictions = new List<Prediction>();
        
        foreach (var nextMatch in fixtures)
        {
            var prediction = Execute(nextMatch, BetType.GoalGoal);
            predictions.Add(prediction);
        }
        
        // Ensure there are enough games for the random selection
        if (predictions.Count < gameCount)
        {
            Console.WriteLine("Not enough games to generate ticket.");
            return null;
        }

        var tickets = new List<Ticket>();
        for (var i = 0; i < ticketCount; i++)
        {
            var finalPredictions = GenerateTicketWithRandomMatches(gameCount, predictions);
            tickets.Add(new Ticket(finalPredictions));
        }
        
        return tickets;
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

    public Prediction Execute(Matches matches, BetType? betType)
    {
        var playedOnDateTime = matches.Date.Parse();
        var historicalData = _historicalMatches.OrderMatchesBy(playedOnDateTime).ToList();
        
        var homeTeamAverage = _dataService.GetTeamMatchAverageBy(historicalData, matches.HomeTeam);
        var awayTeamAverage = _dataService.GetTeamMatchAverageBy(historicalData, matches.AwayTeam);
        var head2HeadAverage = _dataService.HeadToHeadAverageBy(historicalData, matches.HomeTeam, matches.AwayTeam);
        
        var prediction = new Prediction(BetType.Unknown)
        {
            HomeScore = matches.FTHG,
            AwayScore = matches.FTAG,
            Msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam}"
        };

        
        if (matches.HomeTeam == "Liverpool" || matches.HomeTeam == "Chelsea" || 
            matches.HomeTeam == "Crystal Palace" || matches.HomeTeam == "Brescia"|| 
            matches.HomeTeam == "Metz" || matches.HomeTeam == "Oxford" )
        {
                
        }

        var noGoalAnalysis = homeTeamAverage.NoGoalAnalysisBy(awayTeamAverage, head2HeadAverage);
        if (noGoalAnalysis is { Qualified: true })
        {
            return prediction with
            {
                Qualified = true,
                Type = BetType.UnderThreeGoals,
                Percentage = noGoalAnalysis.Probability
            };
        }
        
        var homeWinAnalysis = homeTeamAverage.HomeWin(awayTeamAverage, head2HeadAverage);
        if (homeWinAnalysis is { Qualified: true })
        {
            return prediction with
            {
                Qualified = true,
                Type = BetType.HomeWin,
                Percentage = homeWinAnalysis.Probability
            };
        }
        
        var awayWinAnalysis = homeTeamAverage.AwayWin(awayTeamAverage, head2HeadAverage);
        if (awayWinAnalysis is { Qualified: true })
        {
            return prediction with
            {
                Qualified = true,
                Type = BetType.AwayWin,
                Percentage = awayWinAnalysis.Probability
            };
        }
                
        var twoToThreeGoalsAnalysis = homeTeamAverage.TwoToThreeAnalysisBy(awayTeamAverage, head2HeadAverage);
        if (twoToThreeGoalsAnalysis is { Qualified: true, Probability: > 50 })
        {
            return prediction with
            {
                Qualified = true,
                Type = BetType.TwoToThreeGoals,
                Percentage = twoToThreeGoalsAnalysis.Probability
            };
        }
        
        var goalGoalAnalysis = homeTeamAverage.GoalGoalAnalysisBy(awayTeamAverage, head2HeadAverage);
        if (goalGoalAnalysis is { Qualified: true, Probability: > 60 })
        {
            return prediction with
            {
                Qualified = true,
                Type = BetType.GoalGoal,
                Percentage = goalGoalAnalysis.Probability
            };
        }
        
        var moreThenTwoGoalsAnalysis = homeTeamAverage.MoreThenTwoGoalsAnalysisBy(awayTeamAverage, head2HeadAverage);
        if (moreThenTwoGoalsAnalysis is { Qualified: true, Probability: > 50 })
        {
            return prediction with
            {
                Qualified = true,
                Type = BetType.OverTwoGoals,
                Percentage = moreThenTwoGoalsAnalysis.Probability
            };
        }
        return prediction;
    }
}


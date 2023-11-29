using Accord.IO;
using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class MatchPredictor: IMatchPredictor
{   
    private readonly List<Matches> _historicalMatches;
    private readonly IDataService _dataService;
    private readonly IFileProcessor _fileProcessor;
    
    private double _homeProbability = 0.0;
    private double _awayProbability = 0.0;
    
    private PoissonProbability _current = default!;
    private HeadToHeadData _headToHeadData = default!;
    private TeamData _homeTeamData = default!;
    private TeamData _awayTeamData = default!;
    
    public MatchPredictor(IFileProcessor fileProcessor, IDataService dataService)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
        _dataService = dataService;
        _fileProcessor = fileProcessor;
    }

    public void Execute()
    {
        throw new NotImplementedException();
    }

    public double GetPredictionAccuracyRate(string fixtureName)
    {
        _fileProcessor.CreateFixtureBy("18/08/23", "21/08/23");
        _fileProcessor.CreateFixtureBy("25/08/23", "28/08/23");
        _fileProcessor.CreateFixtureBy("01/09/23", "04/09/23");
        _fileProcessor.CreateFixtureBy("15/09/23", "18/09/23");
        _fileProcessor.CreateFixtureBy("22/09/23", "25/09/23");
        _fileProcessor.CreateFixtureBy("29/09/23", "02/10/23");
        _fileProcessor.CreateFixtureBy("06/10/23", "09/10/23");
        _fileProcessor.CreateFixtureBy("21/10/23", "23/10/23");

        return 0.0;
    }

    public Prediction Execute(Matches matches, BetType? betType)
    {
        var playedOnDateTime = matches.Date.Parse();
        var historicalData = _historicalMatches.OrderMatchesBy(playedOnDateTime).ToList();
        
        _homeTeamData = _dataService.GetTeamDataBy(matches.HomeTeam, historicalData);
        _awayTeamData = _dataService.GetTeamDataBy(matches.AwayTeam, historicalData);
        _headToHeadData = _dataService.GetHeadToHeadDataBy(matches.HomeTeam, matches.AwayTeam, playedOnDateTime);

        var expectedHomeProbability = CalculateProbability(out var expectedAwayProbability);

        if (_homeTeamData.TeamResult.NoGoalGameAvg >= 0.30 || _awayTeamData.TeamResult.NoGoalGameAvg >= 0.30)
        {
            var percentage = _homeTeamData.TeamResult.NoGoalGameAvg + _awayTeamData.TeamResult.NoGoalGameAvg / 2;
            var probability = Math.Exp(-percentage);
            return new Prediction(BetType.UnderThreeGoals) { Qualified = true, Percentage = probability };
        }
        
        return MakePredictionBasedOnProbabilities(expectedHomeProbability, expectedAwayProbability);
    }
    
    private Prediction MakePredictionBasedOnProbabilities(double expectedHomeProbability, double expectedAwayProbability)
    {
        if (expectedHomeProbability < 0.55 || expectedAwayProbability < 0.55)
            return new Prediction(BetType.Unknown); 

        var highestProbability = _homeTeamData.TeamResult.GetHighestProbabilityBy(_awayTeamData.TeamResult, _headToHeadData);
        var probability = GetFilteredHighestProbabilityBy(highestProbability);
        
        return new Prediction(probability.betType)
        {
            Qualified = true, 
            Percentage = probability.probability.Percentage, 
            Msg = highestProbability.Suggestion.Type == probability.probability.Type ? "Second" : ""
        };
    }
    
    private (Probability probability, BetType betType) GetFilteredHighestProbabilityBy(HighestProbability highestProbability)
    {
        var probability = GetBetTypeBy(highestProbability.Suggestion);

        if (probability.betType != BetType.Unknown) 
            return (probability);
        
        foreach (var nextProbability in highestProbability.Probabilities)
        {
            probability = GetBetTypeBy(nextProbability);

            if (probability.betType != BetType.Unknown)
                return probability;
        }

        return probability;
    }
    
    private static bool HasRecentMatchesOfType(TeamData homeTeamData, TeamData awayTeamData, params string[] types) => 
        types.Any(type => homeTeamData.LastThreeGameType.Highest.Type == type && awayTeamData.LastThreeGameType.Highest.Type == type);

    private (Probability probability, BetType betType) GetBetTypeBy(Probability probability)
    {
        switch (probability.Type)
        {
            case "Over Two Goals":
                if (!HasRecentMatchesOfType(_homeTeamData, _awayTeamData, "Over Two Goals"))
                {
                    return (probability, BetType.OverTwoGoals);
                }
                break;
            case "Two to Three Goals":
                if (!HasRecentMatchesOfType(_homeTeamData, _awayTeamData, "Under Three Goals", "Two to Three Goals"))
                {
                    return (probability, BetType.TwoToThreeGoals);
                }
                break;
            case "Goal Goal":
                if (!HasRecentMatchesOfType(_homeTeamData, _awayTeamData, "Goal Goal"))
                {
                    return (probability, BetType.GoalGoal);
                }
                break;
        }

        return (null, BetType.Unknown);
    }
    
    private double GetWinPredictionBy(bool isHome = false)
    {
        var probability = GetProbabilityBy(_awayTeamData.TeamOdds.Win, _awayTeamData.TeamOdds.AwayWin,
            _headToHeadData.Count >= 2 ? _headToHeadData.AwayTeamWon:  0.0);
        
        if (isHome)
        {
            probability = GetProbabilityBy(_homeTeamData.TeamOdds.Win, _homeTeamData.TeamOdds.AwayWin,
                _headToHeadData.Count >= 2 ? _headToHeadData.HomeTeamWon:  0.0);
        }

        return probability;
        

    }

    private static double GetProbabilityBy(double left, double right, double middle = 0.0)
    {
        var percentage = middle == 0.0 ? left + right / 2 : left + middle + right / 3;
        var probability = Math.Exp(-percentage);
        return 1 - probability;
    }


    private Prediction IsQualifiedForGoalGoal(bool riskyOverTwoGoals)
    {
        if (riskyOverTwoGoals)
        {
            if (_homeTeamData.TeamResult.AtLeastOneGoalGameAvg >= 0.40 && _awayTeamData.TeamResult.AtLeastOneGoalGameAvg >= 0.40)
            {
                return new Prediction(BetType.GoalGoal) { Qualified = true };
            }
            
            // Both teams should be able to score and concede goals at least 60%
            if (_homeTeamData is { TeamScoredGames: >= 0.50, TeamConcededGoalGames: >= 0.50 } &&
                _awayTeamData is { TeamScoredGames: >= 0.50, TeamConcededGoalGames: >= 0.50 })
            {
                // one of the both team should suggests Goal Goal and head 2 head must be less then two or also suggests goal goal
                if ((_headToHeadData.Count < 2 || _headToHeadData.Suggestion.Name == "BothTeamScoredGames") &&
                    _homeTeamData.Suggestion.Name == "BothTeamScoredGames" ||
                    _awayTeamData.Suggestion.Name == "BothTeamScoredGames"
                    )
                {
                    var percentage = _homeTeamData.TeamResult.BothScoredGoals +
                        _awayTeamData.TeamResult.BothScoredGoals / 2;
                    
                    return new Prediction(BetType.GoalGoal) 
                    { 
                        Qualified = true, 
                        Percentage = percentage * 1.0
                    };
                }
            }
        }

        return new Prediction(BetType.GoalGoal);
    }
    
    private Prediction IsQualifiedForOverTwoGoals()
    {
        var qualifiedOverTwoGoals = QualifiedOverTwoGoals();
        
        var percentage = _homeTeamData.TeamResult.OverTwoGoals +
                         _awayTeamData.TeamResult.OverTwoGoals / 2;
        
        // Both teams should be able to score and concede goals at least 60%
        if (_homeTeamData is { TeamScoredGames: >= 0.60, TeamConcededGoalGames: >= 0.60 } &&
            _awayTeamData is { TeamScoredGames: >= 0.60, TeamConcededGoalGames: >= 0.60 })
        {
            if (qualifiedOverTwoGoals)
                return new Prediction(BetType.OverTwoGoals) 
                { 
                    Qualified = true, 
                    Percentage = percentage
                };
        }
        
        // At least one teams should be able to score goals over 74% and concede goals over 64%
        if (_homeTeamData is { TeamScoredGames: >= 0.75, TeamConcededGoalGames: >= 0.65 } ||
            _awayTeamData is { TeamScoredGames: >= 0.75, TeamConcededGoalGames: >= 0.65 })
        {
            if (qualifiedOverTwoGoals) 
                return new Prediction(BetType.OverTwoGoals) 
                { 
                    Qualified = true, 
                    Percentage = percentage
                };
        }

        return new Prediction(BetType.OverTwoGoals);
    }
    
    /// <summary>
    /// One of the both team should suggests over two goals and
    /// head 2 head must be less then two or also suggests over two goals
    /// </summary>
    /// <returns></returns>
    private bool QualifiedOverTwoGoals() =>
        (_headToHeadData.Count < 2 || _headToHeadData.Suggestion.Name == "OverScoredGames") &&
        _homeTeamData.Suggestion.Name == "OverScoredGames" || _awayTeamData.Suggestion.Name == "OverScoredGames"; 
    
    private double CalculateProbability(out double expectedAwayProbability)
    {
        var homePower = _homeTeamData.GoalPower;
        var awayPower = _awayTeamData.GoalPower;

        var expectedHomeGoals = (homePower.ScoredGoalProbability + awayPower.ConcededGoalProbability) / 2;
        var expectedAwayGoals = (awayPower.ScoredGoalProbability + homePower.ConcededGoalProbability) / 2;

        var expectedHomeProbability = expectedHomeGoals.GetScoredGoalProbabilityBy();
        expectedAwayProbability = expectedAwayGoals.GetScoredGoalProbabilityBy();
        return expectedHomeProbability;
    }


    /// <summary>
    /// hypothetical formula based on sigmoid activation function (common in neural networks). The idea is that the weighted
    /// average goes through this formula to generate a value between 0 (no chance of scoring) and
    /// 1 (guaranteed to score). We can then multiply by 100 to get a percentage.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SigmoidProbability(double x) => 1 / (1 + Math.Exp(-x));
    

    /// <summary>
    /// Approach to reach linear prediction based on the current season and last six game
    /// </summary>
    /// <param name="performance"></param>
    /// <returns></returns>
    private static double LinearPrediction(TeamGoalAverage performance) =>
        0.60 * performance.Overall.ScoredGoalProbability + 0.40 * performance.Recent.ScoredGoalProbability;
    
    private static double ExponentialPrediction(TeamGoalAverage performance) =>
        Math.Pow(performance.Recent.ScoredGoalProbability, 2) * 0.40 + performance.Overall.ScoredGoalProbability * 0.60;

    private double RatioPrediction(TeamGoalAverage performance)
    {
        // Avoiding division by zero
        if (performance.Overall.ConcededGoalProbability == 0 || performance.Recent.ConcededGoalProbability == 0) 
            return performance.Overall.ScoredGoalProbability + performance.Recent.ScoredGoalProbability;

        var seasonRatio = performance.Overall.ScoredGoalProbability / performance.Overall.ConcededGoalProbability;
        var recentRatio = performance.Recent.ScoredGoalProbability / performance.Recent.ConcededGoalProbability;

        return 0.6 * seasonRatio + 0.4 * recentRatio;
    }
    
    private double CombinedGoalProbability(TeamGoalAverage performance)
    {
        var linear = LinearPrediction(performance);
        var exponential = ExponentialPrediction(performance);
        var ratio = RatioPrediction(performance);

        var rawProbability = (linear + exponential + ratio) / 3;

        // Using the sigmoid to normalize the raw probability to a value between 0 and 1 (0% and 100%)
        return SigmoidProbability(rawProbability) * 100;
    }
    
    private static double LinearPredictionH2H(HeadToHeadGoalAverage performance) =>
        0.60 * performance.AvgGoalsScored + 0.40 * performance.AvgGoalsConceded;

    private static double ExponentialPredictionH2H(HeadToHeadGoalAverage performance) =>
        Math.Pow(performance.AvgGoalsScored, 2) * 0.40 + performance.AvgGoalsConceded * 0.60;

    private double RatioPredictionH2H(HeadToHeadGoalAverage performance)
    {
        // Avoiding division by zero
        if (performance.AvgGoalsConceded == 0) 
            return performance.AvgGoalsScored;

        var ratio = performance.AvgGoalsScored / performance.AvgGoalsConceded;

        return ratio; // In the context of H2H, we may not need to weight the ratios as we did before.
    }

    private double CombinedGoalProbabilityH2H(HeadToHeadGoalAverage performance)
    {
        var linear = LinearPredictionH2H(performance);
        var exponential = ExponentialPredictionH2H(performance);
        var ratio = RatioPredictionH2H(performance);

        var rawProbability = (linear + exponential + ratio) / 3;

        // Using the sigmoid to normalize the raw probability to a value between 0 and 1 (0% and 100%)
        return SigmoidProbability(rawProbability) * 100;
    }


    
    public MatchGoalsData GetTeamSeasonGoals(string home, string away, DateTime playedOnDateTime)
    {
        throw new NotImplementedException();
    }
}


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

    private const double EightPercentageThreshold = 75;
    private const double SeventyPercentageThreshold = 68;
    private const double SixtyPercentageThreshold = 60;
    
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
        
        var homeTeamGoalAvg = _dataService.CalculateTeamGoalAverageBy(matches.HomeTeam, historicalData);
        var awayTeamGoalAvg = _dataService.CalculateTeamGoalAverageBy(matches.AwayTeam, historicalData);
        
        // Define weights for each factor
        _homeProbability = CombinedGoalProbability(homeTeamGoalAvg);
        _awayProbability = CombinedGoalProbability(awayTeamGoalAvg);

        var overTwoGoals = IsOverTwoGoals();
        var bothTeamScoreGoals = IsBothTeamGoals();
        var twoToThreeScoreGoals = IsTwoToThreeGoals();

        if (overTwoGoals.Qualified)
        {
            return overTwoGoals;
        }
        
        if (bothTeamScoreGoals.Qualified)
        {
            return bothTeamScoreGoals;
        }
        
        if (twoToThreeScoreGoals.Qualified)
        {
            return twoToThreeScoreGoals;
        }

        return new Prediction(BetType.Unknown) { Qualified = false };
    }


    private bool IsBothTeamsDefenceWeek() =>
        _homeTeamData.TeamGoals.Total.ConcededProbability > 0.50 && 
        _awayTeamData.TeamGoals.Total.ConcededProbability > 0.50;

    /// <summary>
    /// Analyse the Over two goals chance
    /// </summary>
    /// <returns></returns>
    private Prediction IsOverTwoGoals()
    {
        // Team scoring performance of both team qualified for over two goals
        if (_homeProbability >= EightPercentageThreshold && _awayProbability >= EightPercentageThreshold)
        {
            // Both teams defence is not too strong (Both teams conceded goals are above 50%)
            if (IsBothTeamsDefenceWeek())
            {
                // Both teams has scoring and conceded games over seventy percentage
                if (_awayTeamData.IsScoredGame() && _homeTeamData.IsScoredGame())
                {
                    if (_homeTeamData.TeamResult.UnderTwoGoals < 0.30 && _homeTeamData.TeamResult.OverTwoGoals >= 0.60)
                    {
                        var scoringPower = _homeProbability + _awayProbability / 2;
                        return new Prediction(BetType.OverTwoGoals)
                        {
                            Qualified = true,
                            Percentage = scoringPower,
                            Msg = "Both teams qualified to score over two goals"
                        };
                    }
                }
            }
        }

        return new Prediction(BetType.Unknown);
    }
    
    private Prediction IsBothTeamGoals()
    {
        // Team scoring performance of both team qualified for over two goals
        if (_homeProbability >= SeventyPercentageThreshold && _awayProbability >= SeventyPercentageThreshold)
        {
            // Both teams defence is not too strong (Both teams conceded goals are above 50%)
            if (IsBothTeamsDefenceWeek())
            {
                // Both teams has scoring and conceded games over seventy percentage
                if (_awayTeamData.IsScoredGame() && _homeTeamData.IsScoredGame())
                {
                    if (_homeTeamData.TeamResult is { UnderTwoGoals: < 0.30, BothScoredGoals: >= 0.60 })
                    {
                        var scoringPower = _homeProbability + _awayProbability / 2;
                        return new Prediction(BetType.BothTeamScoreGoals)
                        {
                            Qualified = true,
                            Percentage = scoringPower,
                            Msg = "Both teams qualified to score over two goals"
                        };
                    }
                }
            }
        }

        return new Prediction(BetType.Unknown);
    }

    private Prediction IsTwoToThreeGoals()
    {
        // Team scoring performance of both team qualified for over two goals
        if (_homeProbability >= SixtyPercentageThreshold && _awayProbability >= SixtyPercentageThreshold)
        {
            // Both teams defence is not too strong (Both teams conceded goals are above 50%)
            if (IsBothTeamsDefenceWeek())
            {
                // Both teams has scoring and conceded games over seventy percentage
                if (_awayTeamData.IsScoredGame() && _homeTeamData.IsScoredGame())
                {
                    if (_homeTeamData.TeamResult is { UnderTwoGoals: < 0.30, BothScoredGoals: >= 0.60 })
                    {
                        var scoringPower = _homeProbability + _awayProbability / 2;
                        return new Prediction(BetType.TwoToThreeGoals)
                        {
                            Qualified = true,
                            Percentage = scoringPower,
                            Msg = "Qualified for two to three goals"
                        };
                    }
                }
            }
        }

        return new Prediction(BetType.Unknown);
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
        0.60 * performance.Overall.AvgGoalsScored + 0.40 * performance.Recent.AvgGoalsScored;
    
    private static double ExponentialPrediction(TeamGoalAverage performance) =>
        Math.Pow(performance.Recent.AvgGoalsScored, 2) * 0.40 + performance.Overall.AvgGoalsScored * 0.60;

    private double RatioPrediction(TeamGoalAverage performance)
    {
        // Avoiding division by zero
        if (performance.Overall.AvgGoalsConceded == 0 || performance.Recent.AvgGoalsConceded == 0) 
            return performance.Overall.AvgGoalsScored + performance.Recent.AvgGoalsScored;

        var seasonRatio = performance.Overall.AvgGoalsScored / performance.Overall.AvgGoalsConceded;
        var recentRatio = performance.Recent.AvgGoalsScored / performance.Recent.AvgGoalsConceded;

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


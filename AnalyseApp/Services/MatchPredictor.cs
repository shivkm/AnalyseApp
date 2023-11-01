using Accord.Math;
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

    public double ScoreProbabilityHigh = 0.68;
    public double ConcededProbabilityHigh = 0.80;
    public double ScoreDifference = 0.25;
    
    private PoissonProbability _current = default!;
    private HeadToHeadData _headToHeadData = default!;
    private TeamData _homeTeamData = default!;
    private TeamData _awayTeamData = default!;
    
    private const double FiftyPercentage = 0.50;
    private const double SixtyEightPercentage = 0.68;
    private const double SeventyPercentage = 0.70;
    
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
        var headToHeadGoalAvg = _dataService.CalculateHeadToHeadAverageBy(matches.HomeTeam, matches.AwayTeam, matches.Date.Parse());
        var goalProbabilities = CalculateGoalProbability(homeTeamGoalAvg, awayTeamGoalAvg, headToHeadGoalAvg, matches);

        if (goalProbabilities.Qualified)
        {
            return new Prediction(0.0, goalProbabilities.BetType)
            {
                Qualified = true
            };
        }

        return new Prediction(0.0, BetType.Unknown) { Qualified = false };
        
        var overallScoringChance = GoalChancesInMatch();
        var currentScoringChance = GoalChancesInMatch();
        
        if (overallScoringChance is { Qualified: true, Type: not QualificationType.None } &&
            currentScoringChance is { Qualified: true, Type: not QualificationType.None })
        {
            return new Prediction(0.0, currentScoringChance.BetType)
            {
                Qualified = true
            };
        }

        return new Prediction(0.0, BetType.UnderThreeGoals)
        {
            Qualified = true
        };
    }

    /// <summary>
    /// This method will analyse the scoring and conceding goals based on the teams data
    /// </summary>
    /// <returns></returns>
    private GoalAnalysis GoalChancesInMatch()
    {
        // Teams overall scoring and conceding goals performance
        var homeTotalGoals = _homeTeamData.TeamGoals.Total;
        var awayTotalGoals = _awayTeamData.TeamGoals.Total;
        var homeHomeGoals = _homeTeamData.TeamGoals.Home;
        var awayAwayGoals = _awayTeamData.TeamGoals.Away;
        var homeResult = _homeTeamData.TeamResult;
        var awayResult = _awayTeamData.TeamResult;
        
        var homeTeamGoalsAvg = homeTotalGoals.ScoreProbability.IsGoalsPossible(homeTotalGoals.ConcededProbability);
        var awayTeamGoalsAvg = awayTotalGoals.ScoreProbability.IsGoalsPossible(awayTotalGoals.ConcededProbability);
        
        if (homeTeamGoalsAvg && awayTeamGoalsAvg ||
            homeTotalGoals.ScoreProbability > 0.68 && awayTotalGoals.ConcededProbability > 0.60 ||
            awayTotalGoals.ScoreProbability > 0.68 && homeTotalGoals.ConcededProbability > 0.60)
        {
            if (_awayTeamData.LastThreeMatchResult is BetType.OverTwoGoals or BetType.BothTeamScoreGoals && _awayTeamData.TeamResult.UnderTwoGoals == 0 &&
                _homeTeamData.LastThreeMatchResult is BetType.OverTwoGoals or BetType.BothTeamScoreGoals && _homeTeamData.TeamResult.UnderTwoGoals == 0)
            {
                return new GoalAnalysis(true, QualificationType.Both, BetType.UnderThreeGoals);
            }

            // Home and away teams scoring and conceding goals performance in current collision
            if (homeHomeGoals.ScoreProbability.IsGoalsPossible(homeHomeGoals.ConcededProbability) &&
                awayAwayGoals.ScoreProbability.IsGoalsPossible(awayAwayGoals.ConcededProbability) &&
                _headToHeadData.OverScoredGames >= 0.50
               )
            {
                return new GoalAnalysis(true, QualificationType.Both, BetType.OverTwoGoals);
            }
            if (homeHomeGoals.ScoreProbability.IsGoalsPossible(homeHomeGoals.ConcededProbability) ||
                awayAwayGoals.ScoreProbability.IsGoalsPossible(awayAwayGoals.ConcededProbability) 
               )
            {
                if (homeResult.OverTwoGoals >= 0.50 && awayResult.OverTwoGoals >= 0.50 && _headToHeadData.OverScoredGames >= 0.50)
                {
                    return new GoalAnalysis(true, QualificationType.Both, BetType.OverTwoGoals);
                }
                if (homeResult.BothScoredGoals >= 0.50 && awayResult.BothScoredGoals >= 0.50 && _headToHeadData.OverScoredGames >= 0.50)
                {
                    return new GoalAnalysis(true, QualificationType.Both, BetType.BothTeamScoreGoals);
                }
                if ((homeResult.TwoToThreeGoals >= 0.50 && awayResult.TwoToThreeGoals >= 0.50 ||
                     (homeResult.UnderTwoGoals >= 0.50 || homeResult.TwoToThreeGoals >= 0.50) &&
                     (awayResult.UnderTwoGoals >= 0.50 || awayResult.TwoToThreeGoals >= 0.50)) && _headToHeadData.TwoToThreeGoalsGames >= 0.50 )
                {
                    return new GoalAnalysis(true, QualificationType.Both, BetType.TwoToThreeGoals);
                }
            }

            var homeQualified = homeHomeGoals.ScoreProbability >= 0.68 &&
                                awayAwayGoals.ConcededProbability > FiftyPercentage;

            var awayQualified = awayAwayGoals.ScoreProbability >= 0.68 &&
                                homeHomeGoals.ConcededProbability > FiftyPercentage;

            // If one of the following condition doesn't fit than it has chance to be two to three goals
            if (homeQualified && !awayQualified || !homeQualified && awayQualified)
            {
                if (homeResult.TwoToThreeGoals > FiftyPercentage && awayResult.TwoToThreeGoals > FiftyPercentage)
                {
                    return new GoalAnalysis(true, QualificationType.Both, BetType.TwoToThreeGoals);
                }
            }
        }

        if (homeTeamGoalsAvg || awayTeamGoalsAvg)
        {
            // Combination of this conditions make Chali games
            if ((homeResult.IsChaliGame(_homeTeamData) || homeResult.IsUnPredictableGame(_awayTeamData)) &&
                (awayResult.IsChaliGame(_awayTeamData) || awayResult.IsUnPredictableGame(_awayTeamData)))
            {
                return new GoalAnalysis(true, QualificationType.Both, BetType.OverTwoGoals);
            }
        }
        return new GoalAnalysis(false, QualificationType.None, BetType.Unknown);
    }
    
    /// <summary>
    /// This method will analyse the scoring and conceding goals based on the teams data
    /// </summary>
    /// <returns></returns>
    private GoalAnalysis CalculateGoalProbability(TeamGoalAverage homeTeam, TeamGoalAverage awayTeam, HeadToHeadGoalAverage headToHeadGoalAverage, Matches match)
    {
        // Define weights for each factor
        var homeProbability = CombinedGoalProbability(homeTeam);
        var awayProbability = CombinedGoalProbability(awayTeam);
        var headToHeadGoalProbability = CombinedGoalProbabilityH2H(headToHeadGoalAverage);

        var scoringPower = (homeProbability + awayProbability + headToHeadGoalProbability) / 3;
        const double eightPercentageThreshold = 75;
        const double seventyPercentageThreshold = 70;
        const double sixtyPercentageThreshold = 60;

        var homeGoals = _homeTeamData.TeamGoals;
        var awayGoals = _awayTeamData.TeamGoals;
        var headToHeads = _headToHeadData;
        
        var teamsDefence = homeGoals.Total.ConcededProbability > 0.50 &&
                                awayGoals.Total.ConcededProbability > 0.50;
        
        if (homeProbability >= eightPercentageThreshold && awayProbability >= eightPercentageThreshold && teamsDefence)
        {
            return new GoalAnalysis(true, QualificationType.Both, BetType.OverTwoGoals);
        }
        if (homeProbability >= seventyPercentageThreshold && awayProbability >= seventyPercentageThreshold && teamsDefence)
        {
            return new GoalAnalysis(true, QualificationType.Both, BetType.BothTeamScoreGoals);
        }
        if (homeProbability >= sixtyPercentageThreshold && awayProbability >= sixtyPercentageThreshold && teamsDefence)
        {
            return new GoalAnalysis(true, QualificationType.Both, BetType.TwoToThreeGoals);
        }

        return new GoalAnalysis(true, QualificationType.Both, BetType.UnderThreeGoals);
    }

    /// <summary>
    /// hypothetical formula based on sigmoid activation function (common in neural networks). The idea is that the weighted
    /// average goes through this formula to generate a value between 0 (no chance of scoring) and
    /// 1 (guaranteed to score). We can then multiply by 100 to get a percentage.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static double SigmoidProbability(double x)
    {
        return 1 / (1 + Math.Exp(-x));
    }

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


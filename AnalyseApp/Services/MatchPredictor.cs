using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class MatchPredictor: IMatchPredictor
{   
    private readonly List<Matches> _historicalMatches;
    private readonly IDataService _dataService;
    private readonly IFileProcessor _fileProcessor;

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
        _fileProcessor.CreateFixtureBy("11/08/23", "14/08/23");
        _fileProcessor.CreateFixtureBy("18/08/23", "21/08/23");
        _fileProcessor.CreateFixtureBy("25/08/23", "28/08/23");
        _fileProcessor.CreateFixtureBy("01/09/23", "04/09/23");
        _fileProcessor.CreateFixtureBy("15/09/23", "18/09/23");
        _fileProcessor.CreateFixtureBy("22/09/23", "25/09/23");
        _fileProcessor.CreateFixtureBy("29/09/23", "02/10/23");
        _fileProcessor.CreateFixtureBy("06/10/23", "09/10/23");
        _fileProcessor.CreateFixtureBy("20/10/23", "23/09/23");

        return 0.0;
    }

    public Prediction Execute(Matches matches, BetType? betType)
    {
        var playedOnDateTime = matches.Date.Parse();
        var historicalData = _historicalMatches.OrderMatchesBy(playedOnDateTime).ToList();
        
        _homeTeamData = _dataService.GetTeamDataBy(matches.HomeTeam, historicalData);
        _awayTeamData = _dataService.GetTeamDataBy(matches.AwayTeam, historicalData);
        
        var overallScoringChance = GoalChancesInMatch();
        var currentScoringChance = GoalChancesInMatch();
        var teamsOdds = MatchOddAnalysis();
        
        if (overallScoringChance is { Qualified: true, Type: not QualificationType.None } &&
            currentScoringChance is { Qualified: true, Type: not QualificationType.None })
        {
            return new Prediction(0.0, currentScoringChance.BetType)
            {
                Qualified = true
            };
        }
        if (teamsOdds.Qualified)
        {
            return new Prediction(0.0, teamsOdds.BetType)
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
            if (_homeTeamData is { LastThreeMatchResult: BetType.Unknown, TeamScoredGames: >= 1.0 }  &&
                _awayTeamData is { LastThreeMatchResult: BetType.TwoToThreeGoals, TeamScoredGames: > 0.66 and < 0.90 } ||
                _awayTeamData is { LastThreeMatchResult: BetType.Unknown, TeamScoredGames: >= 0.98 }   &&
                _homeTeamData is { LastThreeMatchResult: BetType.TwoToThreeGoals, TeamScoredGames: > 0.66 and < 0.90 })
            {
                return new GoalAnalysis(true, QualificationType.Both, BetType.TwoToThreeGoals);
            }
            
            if (_homeTeamData is { LastThreeMatchResult: BetType.Unknown, TeamScoredGames: >= 1.0, TeamResult.UnderTwoGoals: 0 } &&
                _awayTeamData.TeamResult.UnderTwoGoals == 0 ||
                _awayTeamData is { LastThreeMatchResult: BetType.Unknown, TeamScoredGames: >= 1.0, TeamResult.UnderTwoGoals: 0 } &&
                _homeTeamData.TeamResult.UnderTwoGoals == 0)
            {
                return new GoalAnalysis(true, QualificationType.Both, BetType.UnderThreeGoals);
            }
            
            // Home and away teams scoring and conceding goals performance in current collision
            if (homeHomeGoals.ScoreProbability.IsGoalsPossible(homeHomeGoals.ConcededProbability) &&
                awayAwayGoals.ScoreProbability.IsGoalsPossible(awayAwayGoals.ConcededProbability) 
               )
            {
                return new GoalAnalysis(true, QualificationType.Both, BetType.OverTwoGoals);
            }
            if (homeHomeGoals.ScoreProbability.IsGoalsPossible(homeHomeGoals.ConcededProbability) ||
                awayAwayGoals.ScoreProbability.IsGoalsPossible(awayAwayGoals.ConcededProbability) 
               )
            {
                if (homeResult.OverTwoGoals >= 0.66 && awayResult.OverTwoGoals >= 0.66)
                {
                    return new GoalAnalysis(true, QualificationType.Both, BetType.OverTwoGoals);
                }
                if (homeResult.BothScoredGoals >= 0.66 && awayResult.BothScoredGoals >= 0.66)
                {
                    return new GoalAnalysis(true, QualificationType.Both, BetType.BothTeamScoreGoals);
                }
                if (homeResult.TwoToThreeGoals >= 0.66 && awayResult.TwoToThreeGoals >= 0.66 ||
                    (homeResult.UnderTwoGoals >= 0.50 || homeResult.TwoToThreeGoals >= 0.66) &&
                    (awayResult.UnderTwoGoals >= 0.50 || awayResult.TwoToThreeGoals >= 0.66))
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
    private GoalAnalysis MatchOddAnalysis()
    {
        // Teams overall scoring and conceding goals performance
        var homeTotalGoals = _homeTeamData.TeamGoals.Total;
        var awayTotalGoals = _awayTeamData.TeamGoals.Total;
        var homeHomeGoals = _homeTeamData.TeamGoals.Home;
        var awayAwayGoals = _awayTeamData.TeamGoals.Away;
        var homeOdds = _homeTeamData.TeamOdds;
        var awayOdds = _awayTeamData.TeamOdds;
        
        // Teams overall scoring and conceding goals performance
        var homeTeamGoalsAvg = homeTotalGoals.ScoreProbability.IsGoalsPossible(homeTotalGoals.ConcededProbability);
        var awayTeamGoalsAvg = awayTotalGoals.ScoreProbability.IsGoalsPossible(awayTotalGoals.ConcededProbability);

        if (homeTeamGoalsAvg && awayTeamGoalsAvg ||
            homeTotalGoals.ScoreProbability > 0.68 && awayTotalGoals.ConcededProbability > 0.80 ||
            awayTotalGoals.ScoreProbability > 0.68 && homeTotalGoals.ConcededProbability > 0.80
        )
        {
            if (homeHomeGoals.ScoreProbability > awayAwayGoals.ScoreProbability &&
                homeHomeGoals.ScoreProbability - awayAwayGoals.ScoreProbability > 0.25 &&
                homeHomeGoals.ConcededProbability < awayAwayGoals.ConcededProbability
               )
            {
                return new GoalAnalysis(true, QualificationType.Home, BetType.HomeWin);
            }  
            
            if (awayAwayGoals.ScoreProbability > homeHomeGoals.ScoreProbability &&
                awayAwayGoals.ScoreProbability - homeHomeGoals.ScoreProbability > 0.25 &&
                awayAwayGoals.ConcededProbability < homeHomeGoals.ConcededProbability
               )
            {
                return new GoalAnalysis(true, QualificationType.Away, BetType.AwayWin);
            }
        }

        if (homeTeamGoalsAvg && 
            awayTotalGoals is { ScoreProbability: < FiftyPercentage, ConcededProbability: < FiftyPercentage })
        {
            if (homeHomeGoals.ScoreProbability.IsGoalsPossible(homeHomeGoals.ConcededProbability) &&
                awayAwayGoals is { ScoreProbability: < FiftyPercentage, ConcededProbability: < FiftyPercentage })
            {
                
                return new GoalAnalysis(true, QualificationType.Home, BetType.HomeWin);
            }
        }
        if (awayTeamGoalsAvg && 
            homeHomeGoals is { ScoreProbability: < FiftyPercentage, ConcededProbability: < FiftyPercentage })
        {
            if (awayAwayGoals.ScoreProbability.IsGoalsPossible(awayAwayGoals.ConcededProbability) &&
                homeHomeGoals is { ScoreProbability: < FiftyPercentage, ConcededProbability: < FiftyPercentage })
            {
                
                return new GoalAnalysis(true, QualificationType.Away, BetType.AwayWin);
            }
        }
        return new GoalAnalysis(false, QualificationType.None, BetType.Unknown);
    }

    private GoalAnalysis   RiskyGamesAnalysis()
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
            if (_homeTeamData is { LastThreeMatchResult: BetType.Unknown, TeamScoredGames: >= 1.0 }  &&
                _awayTeamData is { LastThreeMatchResult: BetType.TwoToThreeGoals, TeamScoredGames: > 0.66 and < 0.90 } ||
                _awayTeamData is { LastThreeMatchResult: BetType.Unknown, TeamScoredGames: >= 0.98 }   &&
                _homeTeamData is { LastThreeMatchResult: BetType.TwoToThreeGoals, TeamScoredGames: > 0.66 and < 0.90 })
            {
                return new GoalAnalysis(true, QualificationType.Both, BetType.TwoToThreeGoals);
            }
            
            if (_homeTeamData is { LastThreeMatchResult: BetType.Unknown, TeamScoredGames: >= 1.0, TeamResult.UnderTwoGoals: 0 } &&
                _awayTeamData.TeamResult.UnderTwoGoals == 0 ||
                _awayTeamData is { LastThreeMatchResult: BetType.Unknown, TeamScoredGames: >= 1.0, TeamResult.UnderTwoGoals: 0 } &&
                _homeTeamData.TeamResult.UnderTwoGoals == 0)
            {
                return new GoalAnalysis(true, QualificationType.Both, BetType.UnderThreeGoals);
            }
        }
        
        return new GoalAnalysis(false, QualificationType.None, BetType.Unknown);
    }
    
    public MatchGoalsData GetTeamSeasonGoals(string home, string away, DateTime playedOnDateTime)
    {
        throw new NotImplementedException();
    }
}


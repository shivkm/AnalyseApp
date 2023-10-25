using Accord;
using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class MatchPredictor: IMatchPredictor
{   
    private readonly List<Matches> _historicalMatches;
    private readonly IPoissonService _poissonService;
    private readonly IDataService _dataService;
    private readonly IFileProcessor _fileProcessor;

    private PoissonProbability _current = default!;
    private HeadToHeadData _headToHeadData = default!;
    private TeamData _homeTeamData = default!;
    private TeamData _awayTeamData = default!;
    
    private const double FiftyPercentage = 0.50;
    private const double SixtyEightPercentage = 0.68;
    private const double SeventyPercentage = 0.70;
    public MatchPredictor(IFileProcessor fileProcessor, IPoissonService poissonService, IDataService dataService)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
        _poissonService = poissonService;
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
        
        var homeSeasonGoals = _homeTeamData.SeasonTeamGoals;
        var awaySeasonGoals = _awayTeamData.SeasonTeamGoals;
        var overallScoringChance = GoalChancesInMatch(homeSeasonGoals, awaySeasonGoals);
        
        var homeGoals = _homeTeamData.TeamGoals;
        var awayGoals = _awayTeamData.TeamGoals;
        var currentScoringChance = GoalChancesInMatch(homeGoals, awayGoals);

        var teamsOdds = MatchOddAnalysis();
        if ((overallScoringChance is { Qualified: true, Type: not QualificationType.None } || 
             homeSeasonGoals.Home.MatchCount <= 3 && awaySeasonGoals.Away.MatchCount <= 3) &&
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
        else
        {
            return new Prediction(0.0, BetType.UnderThreeGoals)
            {
                Qualified = true
            };
        }       
    }

    /// <summary>
    /// This method will analyse the scoring and conceding goals based on the teams data
    /// </summary>
    /// <param name="home">Home team goal data</param>
    /// <param name="away">Away team goal data</param>
    /// <returns></returns>
    private GoalAnalysis GoalChancesInMatch(TeamGoals home, TeamGoals away)
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
            homeTotalGoals.ScoreProbability > 0.68 && awayTotalGoals.ConcededProbability > 0.80 ||
            awayTotalGoals.ScoreProbability > 0.68 && homeTotalGoals.ConcededProbability > 0.80)
        {
            // Home and away teams scoring and conceding goals performance in current collision
            if (homeHomeGoals.ScoreProbability.IsGoalsPossible(homeHomeGoals.ConcededProbability) &&
                awayAwayGoals.ScoreProbability.IsGoalsPossible(awayAwayGoals.ConcededProbability) 
               )
            {
                return new GoalAnalysis(true, QualificationType.Both, BetType.OverTwoGoals);
            }

            var homeQualified = homeHomeGoals.ScoreProbability >= 0.68 &&
                                awayAwayGoals.ConcededProbability > FiftyPercentage;

            var awayQualified = awayAwayGoals.ScoreProbability >= 0.68 &&
                                homeHomeGoals.ConcededProbability > FiftyPercentage;
            
            if (homeHomeGoals.ScoreProbability.IsGoalsPossible(homeHomeGoals.ConcededProbability) ||
                ((homeQualified || awayQualified) &&
                 homeResult.OverTwoGoals >= 0.68 && awayResult.OverTwoGoals >= 0.68)
                )
            {
                var qualifiedType = homeQualified ? QualificationType.Home : QualificationType.Away;
                return new GoalAnalysis(true, qualifiedType, BetType.OverTwoGoals);
            }
            
            if (away.Away.ScoreProbability.IsGoalsPossible(away.Away.ConcededProbability) &&
                (home.Home.ConcededProbability > 0.60 || home.Home.ScoreProbability > FiftyPercentage))
            {
                return new GoalAnalysis(true, QualificationType.Away, BetType.OverTwoGoals);
            }
        }
        
        return new GoalAnalysis(false, QualificationType.None, BetType.Unknown);
    }
    
        /// <summary>
    /// This method will analyse the scoring and conceding goals based on the teams data
    /// </summary>
    /// <param name="home">Home team goal data</param>
    /// <param name="away">Away team goal data</param>
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
        
        return new GoalAnalysis(false, QualificationType.None, BetType.Unknown);
    }
    

    public MatchGoalsData GetTeamSeasonGoals(string home, string away, DateTime playedOnDateTime)
    {
        throw new NotImplementedException();
    }
}


using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class DataService(IFileProcessor fileProcessor) : IDataService
{
    private readonly List<Matches> _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
    
    private const double ForthPercentage = 0.40;
    private const double SixtyPercentage = 0.60;

    /// <summary>
    /// Calculate the average of the team scored in historical matches
    /// </summary>
    /// <param name="team"></param>
    /// <returns></returns>
    public Match GetTeamMatchAverageBy(List<Matches> historicalMatches, string team)
    {
        var teamHistoricalMatches = _historicalMatches
            .GetMatchesBy(match => match.HomeTeam == team || match.AwayTeam == team);
        
        var league = teamHistoricalMatches[0].Div;
        var currentSeasonMatches = _historicalMatches.GetCurrentLeagueBy(2023, league).ToList();
        var teamHomeMatches = teamHistoricalMatches.GetMatchesBy(match => match.HomeTeam == team);
        var teamAwayMatches = teamHistoricalMatches.GetMatchesBy(match => match.AwayTeam == team);
        
        var overallAverage = GetMatchAverageBy(teamHistoricalMatches, team);
        var currentSeasonAverage = GetMatchAverageBy(currentSeasonMatches, team);
        var homeAverage = GetMatchAverageBy(teamHomeMatches, team, true, true);
        var awayAverage = GetMatchAverageBy(teamAwayMatches, team, true);
        var finalAverage = GetMergeAverage(overallAverage, currentSeasonAverage, homeAverage, awayAverage);
        
        return new Match
        {
            HomeAverage = homeAverage,
            HomeOverallAverage = finalAverage,
            
            AwayAverage = awayAverage,
            AwayOverallAverage = finalAverage
        };
    }
    
    /// <summary>
    /// Calculate teams head to head average based on historical matches
    /// </summary>
    /// <param name="homeTeam"></param>
    /// <param name="awayTeam"></param>
    /// <returns></returns>
    public Head2HeadAverage HeadToHeadAverageBy(List<Matches> historicalMatches, string homeTeam, string awayTeam)
    {
        var h2hMatches = _historicalMatches
            .GetMatchesBy(a => a.HomeTeam == homeTeam && a.AwayTeam == awayTeam ||
                                        a.AwayTeam == homeTeam && a.HomeTeam == awayTeam);

        var homeScoringPower = h2hMatches.GoalAverage(true).GetScoredGoalProbabilityBy();
        var awayScoringPower = h2hMatches.GoalAverage().GetScoredGoalProbabilityBy();
        var probability = homeScoringPower * 0.50 + awayScoringPower * 0.50;
        
        var result = new Head2HeadAverage
        {
            Count = h2hMatches.Count,
            ZeroZero = h2hMatches.ZeroZeroGoal(),
            BothTeamScore = h2hMatches.BothTeamMakeGoal(),
            MoreThanTwoGoals = h2hMatches.MoreThanTwoGoals(),
            TwoToThree = h2hMatches.TwoToThreeGoals(),
            HomWin = h2hMatches.Win(homeTeam),
            AwayWin = h2hMatches.Win(awayTeam),
            Draw = h2hMatches.Draw(),
            PoissonProbability = probability
        };

        return result;
    }

    private static MatchAverage GetMatchAverageBy(IList<Matches> matches, string team, bool field = false, bool isHome = false)
    {
        var homeOneGoal = matches.GetMoreThenGivenGoalPercentageBy(true, 0);
        var awayOneGoal = matches.GetMoreThenGivenGoalPercentageBy(false, 0);
        var finalOneGoal = homeOneGoal * 0.50 + awayOneGoal * 0.50;
        
        var homeGoalAverage = matches.GoalAverage(true);
        var awayGoalAverage = matches.GoalAverage();
        var finalGoalAverage = homeGoalAverage + awayGoalAverage;
        
        var homeProbability = homeGoalAverage.GetScoredGoalProbabilityBy();
        var awayProbability = awayGoalAverage.GetScoredGoalProbabilityBy();
        var probability = finalGoalAverage.GetScoredGoalProbabilityBy();
        
        var matchAverage = new MatchAverage
        {
            AtLeastOneGoal = field ? isHome ? homeOneGoal : awayOneGoal : finalOneGoal,
            ZeroZero = matches.ZeroZeroGoal(),
            MoreThanTwoGoals = matches.MoreThanTwoGoals(),
            TwoToThree = matches.TwoToThreeGoals(),
            Win = matches.Win(team),
            Loss = matches.Loss(team),
            Draw = matches.Draw(),
            PoissonProbability = field ? isHome ? homeProbability : awayProbability : probability,
        };

        return matchAverage;
    }

    private static MatchAverage GetMergeAverage(MatchAverage left, MatchAverage right)
    {
        var matchAverage = new MatchAverage
        {
            AtLeastOneGoal = left.AtLeastOneGoal * ForthPercentage + right.AtLeastOneGoal * SixtyPercentage,
            ZeroZero = left.ZeroZero * ForthPercentage + right.ZeroZero * SixtyPercentage,
            MoreThanTwoGoals = left.MoreThanTwoGoals *  ForthPercentage + right.MoreThanTwoGoals * SixtyPercentage,
            TwoToThree = left.TwoToThree * ForthPercentage + right.TwoToThree * SixtyPercentage,
            Win = left.Win * ForthPercentage + right.Win * SixtyPercentage,
            Loss = left.Loss * ForthPercentage + right.Loss * SixtyPercentage,
            Draw = left.Draw * ForthPercentage + right.Draw * SixtyPercentage,
            PoissonProbability = left.PoissonProbability * ForthPercentage + right.PoissonProbability * SixtyPercentage
        };

        return matchAverage;
    }
    
    private static MatchAverage GetMergeAverage(MatchAverage left, MatchAverage right, MatchAverage up, MatchAverage down)
    {
        var matchAverage = new MatchAverage
        {
            AtLeastOneGoal = left.AtLeastOneGoal.CalculateAveragePercentage(right.AtLeastOneGoal, up.AtLeastOneGoal, down.AtLeastOneGoal),
            ZeroZero = left.ZeroZero.CalculateAveragePercentage(right.ZeroZero, up.ZeroZero, down.ZeroZero),
            MoreThanTwoGoals = left.MoreThanTwoGoals.CalculateAveragePercentage(right.MoreThanTwoGoals, up.MoreThanTwoGoals, down.MoreThanTwoGoals),
            TwoToThree = left.TwoToThree.CalculateAveragePercentage(right.TwoToThree, up.TwoToThree, down.TwoToThree),
            Win = left.Win.CalculateAveragePercentage(right.Win, up.Win, down.Win),
            Loss = left.Loss.CalculateAveragePercentage(right.Loss, up.Loss, down.Loss),
            Draw = left.Draw.CalculateAveragePercentage(right.Draw, up.Draw, down.Draw),
            PoissonProbability = left.PoissonProbability.CalculateAveragePercentage(right.PoissonProbability, up.PoissonProbability, down.PoissonProbability),
        };

        return matchAverage;
    }
}
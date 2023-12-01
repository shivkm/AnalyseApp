using AnalyseApp.Enums;
using AnalyseApp.models;
using AnalyseApp.Models;

namespace AnalyseApp.Extensions;

internal static class MatchesExtensions
{
    internal static IEnumerable<Matches> OrderMatchesBy(this List<Matches> matches, DateTime playDate)
    {
        matches = matches.Where(i =>
            {
                var matchDate = i.Date.Parse();
                return matchDate < playDate;
            })
            .OrderByDescending(i => i.Date.Parse())
            .ToList();

        return matches;
    }
    
    internal static IEnumerable<Matches> GetCurrentLeagueBy(this List<Matches> matches, int currentSeasonYear, string league)
    {
        var formatStartDate = $"20/07/{currentSeasonYear}";
        var foundMatches = matches
                .Where(i => i.Div == league)
                .Where(i =>
                {
                    var matchDate = i.Date.Parse();
                    return matchDate > formatStartDate.Parse();
                })
                .OrderByDescending(i => i.Date.Parse())
                .ToList();

        return foundMatches;
    } 

    internal static IEnumerable<Matches> GetHeadToHeadMatchesBy(this List<Matches> premierLeagueGames, string homeTeam, string awayTeam, DateTime playedOn)
    {
        var homeMatches = premierLeagueGames
            .Where(i =>
            {
                var matchDate = i.Date.Parse();
                return matchDate < playedOn;
            })
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                        i.AwayTeam == homeTeam && i.HomeTeam == awayTeam)
            .ToList();
        
        return homeMatches;
    }
    
    internal static double GetGameAvgBy(this IEnumerable<Matches> matches, int count, Func<Matches, bool> predictor) =>
        matches.Count(predictor) / (double)count;
    
    public static HighestProbability GetHighestProbabilityBy(this TeamResult homeTeam, TeamResult awayTeam, HeadToHeadData headToHeadData)
    {
        var goalGoalProbability =  homeTeam.AtLeastOneGoalGameAvg.GetProbabilityBy(
            awayTeam.AtLeastOneGoalGameAvg,
            headToHeadData.GetHead2HeadValueBy("GoalGoal")
            );
        
        var overTwoGoalsProbability = homeTeam.OverTwoGoals.GetProbabilityBy(
            awayTeam.OverTwoGoals,
            headToHeadData.GetHead2HeadValueBy("OverTwoGoals")
            );
        
        var twoToThreeGoalProbability = homeTeam.TwoToThreeGoals.GetProbabilityBy(
            awayTeam.TwoToThreeGoals,
            headToHeadData.GetHead2HeadValueBy("TwoToThreeGoals")
            );

        var highestProbabilityOutcome = overTwoGoalsProbability.GetHighestProbability(
            goalGoalProbability, 
            twoToThreeGoalProbability
        );

        return highestProbabilityOutcome;
    }

    private static double GetHead2HeadValueBy(this HeadToHeadData headToHead, string type)
    {
        if (headToHead.Count < 2) return 0.0;

        return type switch
        {
            "GoalGoal" => headToHead.BothTeamScoredGames,
            "OverTwoGoals" => headToHead.OverScoredGames,
            "UnderThreeGoals" => headToHead.UnderThreeScoredGames,
            "TwoToThreeGoals" => headToHead.TwoToThreeGoalsGames,
            _ => 0.0
        };
    }
    
    
    private static double GetProbabilityBy(this double left, double right, double middle = 0.0)
    {
        var percentage = middle == 0.0 ? left + right / 2 : left + middle + right / 3;
        var probability = Math.Exp(-percentage);
        return 1 - probability;
    }
    
    private static HighestProbability GetHighestProbability(this double overTwoGoals, double goalGoal, double twoToThreeGoal)
    {
        var probabilities = new List<Probability>
        {
            new("Over Two Goals", overTwoGoals),
            new("Goal Goal", goalGoal),
            new("Two to Three Goals", twoToThreeGoal)
        };

        var orderByProbability = probabilities.OrderByDescending(o => o.Percentage).ToList();
        
        return new HighestProbability(
            orderByProbability.First(),
            orderByProbability
        );
    } 
    
    public static BetType ToBetType(this string type)
    {
        return type switch
        {
            "Over Two Goals" => BetType.OverTwoGoals,
            "Two to Three Goals" => BetType.TwoToThreeGoals,
            "Goal Goal" => BetType.GoalGoal,
            _ => BetType.Unknown,
        };
    }
    
    internal static MatchAverage TeamPerformance(
        this IList<Matches> matches,
        string team,
        bool isHome
    )
    {
        var matchAverage = new MatchAverage();
        var teamHistoricalMatches = matches.GetMatchesBy(a => isHome ? a.HomeTeam == team : a.AwayTeam == team);
        
        matchAverage.AtLeastOneGoal = teamHistoricalMatches.GetExpectedGoalPercentageBy(isHome, 1);
        matchAverage.ZeroZero = teamHistoricalMatches.ZeroZeroGoal();
        matchAverage.MoreThanTwoGoals = teamHistoricalMatches.GetExpectedGoalPercentageBy(isHome, 2);
        matchAverage.TwoToThree = teamHistoricalMatches.TwoToThreeGoals();
        matchAverage.Win = teamHistoricalMatches.Win(team);
        matchAverage.Loss = teamHistoricalMatches.Loss(team);
        matchAverage.Draw = teamHistoricalMatches.Draw();
        matchAverage.PoissonProbability = teamHistoricalMatches.GoalAverage(isHome).GetScoredGoalProbabilityBy();
        matchAverage.ScoreGoalsProbability = 100.0 * teamHistoricalMatches.GoalAverage(isHome);
        matchAverage.ScoreGoalsAverage = teamHistoricalMatches.GoalAverage(isHome);
        
        return matchAverage;
    }
    
    internal static Head2HeadAverage HeadToHeadPerformance(
        this IList<Matches> matches, 
        string homeTeam,
        string awayTeam
    )
    {
        var historicalMatches = matches
            .GetMatchesBy(a => a.HomeTeam == homeTeam && a.AwayTeam == awayTeam ||
                                       a.AwayTeam == homeTeam && a.HomeTeam == awayTeam);

        var homeScoringPower = historicalMatches.GoalAverage(true).GetScoredGoalProbabilityBy();
        var awayScoringPower = historicalMatches.GoalAverage().GetScoredGoalProbabilityBy();
        var probability = homeScoringPower * 0.50 + awayScoringPower * 0.50;
        
        var result = new Head2HeadAverage
        {
            ZeroZero = historicalMatches.ZeroZeroGoal(),
            BothTeamScore = historicalMatches.BothTeamMakeGoal(),
            MoreThanTwoGoals = historicalMatches.MoreThanTwoGoals(),
            TwoToThree = historicalMatches.TwoToThreeGoals(),
            HomWin = historicalMatches.Win(homeTeam),
            AwayWin = historicalMatches.Win(awayTeam),
            Draw = historicalMatches.Draw(),
            PoissonProbability = probability
        };

        return result;
    }
    
    private static IList<Matches> GetMatchesBy(this IEnumerable<Matches> games, Func<Matches, bool> predicate)
    {
        var currentSession = games
            .Where(predicate)
            .ToList();

        return currentSession;
    }
    
    private static double GetExpectedGoalPercentageBy(
        this IList<Matches> matches,
        bool isHome, 
        int expectedGoal
    )
    {
        var result = matches
            .Percent(p => isHome ? p.FTHG >= expectedGoal : p.FTAG >= expectedGoal);

        return result;
    }
    
    private static double MoreThanTwoGoals(this IEnumerable<Matches> matches) => 
        matches.Percent(a =>  a.FTHG + a.FTAG >= 3);
    
    private static double TwoToThreeGoals(this IEnumerable<Matches> matches) =>
        matches.Percent(a => a.FTHG + a.FTAG <= 3 && a.FTHG + a.FTAG > 1);
    
    private static double ZeroZeroGoal(this IEnumerable<Matches> matches) =>
        matches.Percent(p =>  p is { FTHG: 0, FTAG: 0 });

    private static double BothTeamMakeGoal(this IEnumerable<Matches> matches) => 
        matches.Percent(a => a is { FTHG: > 0, FTAG: > 0 });
    
    private static double Win(this IEnumerable<Matches> matches, string teamName) =>
        matches.Percent(a => a.FTHG > a.FTAG && a.HomeTeam == teamName ||
                                    a.FTAG > a.FTHG && a.AwayTeam == teamName);
    
    private static double Loss(this IEnumerable<Matches> matches, string teamName) =>
        matches.Percent(a => a.FTHG < a.FTAG && a.HomeTeam == teamName || a.FTAG < a.FTHG && a.AwayTeam == teamName);
    
    private static double Draw(this IEnumerable<Matches> matches) => matches.Percent(a => a.FTHG == a.FTAG);
    
    private static double GoalAverage(this IEnumerable<Matches> matches, bool isHome = false)
    {
        var average = matches.Average(i => isHome ? i.FTHG : i.FTAG).GetValueOrDefault();
        return average;
    }
    
    public static (bool Qualified, double Probability) TeamNoGoalProbabilityHigh(this MatchAverage currentAverage, MatchAverage overallAverage)
    {
        var probability = currentAverage.ZeroZero.ProbabilityBy(overallAverage.ZeroZero, 20);
        return probability;
    }
    
    public static (bool Qualified, double Probability) TeamOneGoalProbabilityHigh(this MatchAverage currentAverage, MatchAverage overallAverage)
    {
        var probability = currentAverage.AtLeastOneGoal.ProbabilityBy(overallAverage.AtLeastOneGoal);
        return probability;
    }
        
    public static (bool Qualified, double Probability) TeamThenTwoGoalsProbabilityHigh(this MatchAverage currentAverage, MatchAverage overallAverage)
    {
        var probability = currentAverage.MoreThanTwoGoals.ProbabilityBy(overallAverage.MoreThanTwoGoals);
        return probability;
    }
    
    public static (bool Qualified, double Probability) TeamTwoToThreeProbabilityHigh(this MatchAverage currentAverage, MatchAverage overallAverage)
    {
        var probability = currentAverage.TwoToThree.ProbabilityBy(overallAverage.TwoToThree);
        return probability;
    }
    
    public static (bool Qualified, double Probability) ProbabilityBy(this double? currentAverage, double? overallAverage, int passingProbability = 65)
    {
        // this will avoid dividing the value with 0
        var dividingValue = currentAverage.GetValueOrDefault() is 0.0 || overallAverage.GetValueOrDefault() is 0.0 ? 1 : 2;
        passingProbability = currentAverage.GetValueOrDefault() is 0.0 || overallAverage.GetValueOrDefault() is 0.0 ? passingProbability + 15 : passingProbability;
        var probability = (currentAverage.GetValueOrDefault() + overallAverage.GetValueOrDefault())/ dividingValue;
        
        return (probability > passingProbability, probability);
    }
}
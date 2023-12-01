using AnalyseApp.Enums;
using AnalyseApp.models;
using AnalyseApp.Models;

namespace AnalyseApp.Extensions;

internal static class MatchesExtensions
{

    private const double SixthEightPercentage = 68;
    private const double SixthFivePercentage = 65;
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
    
    
    internal static IList<Matches> GetMatchesBy(this IEnumerable<Matches> games, Func<Matches, bool> predicate)
    {
        var currentSession = games
            .Where(predicate)
            .ToList();

        return currentSession;
    }
    
    public static double GetMoreThenGivenGoalPercentageBy(
        this IList<Matches> matches,
        bool isHome, 
        int expectedGoal
    )
    {
        var result = matches
            .Percent(p => isHome ? p.FTHG > expectedGoal : p.FTAG > expectedGoal);

        return result;
    }
    
    public static double MoreThanTwoGoals(this IEnumerable<Matches> matches) => 
        matches.Percent(a =>  a.FTHG + a.FTAG >= 3);
    
    public static double TwoToThreeGoals(this IEnumerable<Matches> matches) =>
        matches.Percent(a => a.FTHG + a.FTAG == 3 || a.FTHG + a.FTAG == 2);
    
    public static double ZeroZeroGoal(this IEnumerable<Matches> matches) =>
        matches.Percent(p =>  p is { FTHG: 0, FTAG: 0 });

    public static double BothTeamMakeGoal(this IEnumerable<Matches> matches) => 
        matches.Percent(a => a is { FTHG: > 0, FTAG: > 0 });
    
    public static double Win(this IEnumerable<Matches> matches, string teamName) =>
        matches.Percent(a => a.FTHG > a.FTAG && a.HomeTeam == teamName ||
                                    a.FTAG > a.FTHG && a.AwayTeam == teamName);
    
    public static double Loss(this IEnumerable<Matches> matches, string teamName) =>
        matches.Percent(a => a.FTHG < a.FTAG && a.HomeTeam == teamName || a.FTAG < a.FTHG && a.AwayTeam == teamName);
    
    public static double Draw(this IEnumerable<Matches> matches) => matches.Percent(a => a.FTHG == a.FTAG);
    
    public static double GoalAverage(this IEnumerable<Matches> matches, bool isHome = false)
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

    public static MatchPrediction GoalGoalAnalysisBy(this Match home, Match away, Head2HeadAverage head2Head)
    {
        var homeAverage = home.HomeAverage;
        var homeOverallAverage = home.HomeOverallAverage;
        var awayAverage = away.AwayAverage;
        var awayOverallAverage = away.AwayOverallAverage;

        var matchPrediction = new MatchPrediction(false, 0.0);
        var probability = CalculateAveragePercentage(
            homeAverage.AtLeastOneGoal,
            awayAverage.AtLeastOneGoal,
            homeOverallAverage.AtLeastOneGoal,
            awayOverallAverage.AtLeastOneGoal
        );
        
        if (probability > SixthEightPercentage &&
            homeAverage.PoissonProbability > 0.65 &&
            awayAverage.PoissonProbability > 0.65 &&
            head2Head.BothTeamScore > 50)
        {
            return matchPrediction with
            {
                Probability = probability,
                Qualified = true
            };
        }
        
        if (probability > SixthEightPercentage - 10 &&
            (homeAverage.PoissonProbability >= 0.70 ||
             awayAverage.PoissonProbability >= 0.55) &&
            head2Head.BothTeamScore >= 60)
        {
            return matchPrediction with
            {
                Probability = probability,
                Qualified = true
            };
        }

        return matchPrediction;
    }
    
    public static MatchPrediction TwoToThreeAnalysisBy(this Match home, Match away, Head2HeadAverage head2Head)
    {
        var homeAverage = home.HomeAverage;
        var homeOverallAverage = home.HomeOverallAverage;
        var awayAverage = away.AwayAverage;
        var awayOverallAverage = away.AwayOverallAverage;

        var matchPrediction = new MatchPrediction(false, 0.0);
        var probability = CalculateAveragePercentage(
            homeAverage.TwoToThree,
            awayAverage.TwoToThree,
            homeOverallAverage.TwoToThree,
            awayOverallAverage.TwoToThree
        );
        
        if (probability > 55 &&
            homeAverage.PoissonProbability > 0.60 &&
            awayAverage.PoissonProbability > 0.60 &&
            homeOverallAverage.AtLeastOneGoal < 70 &&
            awayOverallAverage.AtLeastOneGoal < 70)
        {
            return matchPrediction with
            {
                Probability = probability,
                Qualified = true
            };
        }
        
        if (probability > 55 &&
            (homeAverage.PoissonProbability is <= 0.80 and > 0.50 ||
             awayAverage.PoissonProbability is <= 0.80 and > 0.50 ) &&
            head2Head.TwoToThree >= 60 &&
            homeOverallAverage.AtLeastOneGoal < 70 &&
            awayOverallAverage.AtLeastOneGoal < 70)
        {
            return matchPrediction with
            {
                Probability = probability,
                Qualified = true
            };
        }

        return matchPrediction;
    }
    
    public static MatchPrediction MoreThenTwoGoalsAnalysisBy(this Match home, Match away, Head2HeadAverage head2Head)
    {
        var homeAverage = home.HomeAverage;
        var homeOverallAverage = home.HomeOverallAverage;
        var awayAverage = away.AwayAverage;
        var awayOverallAverage = away.AwayOverallAverage;

        var matchPrediction = new MatchPrediction(false, 0.0);
        var probability = CalculateAveragePercentage(
            homeAverage.MoreThanTwoGoals,
            awayAverage.MoreThanTwoGoals,
            homeOverallAverage.MoreThanTwoGoals,
            awayOverallAverage.MoreThanTwoGoals
        );
        
        if (probability > SixthEightPercentage &&
            (homeAverage.PoissonProbability > 0.65 && awayAverage.PoissonProbability > 0.65 ||
             homeAverage.PoissonProbability > 0.74 || awayAverage.PoissonProbability > 0.74))
        {
            return matchPrediction with
            {
                Probability = probability,
                Qualified = true
            };
        }
        
        if (probability > SixthEightPercentage - 15 &&
            (homeAverage.PoissonProbability > 0.64 && awayAverage.PoissonProbability > 0.64 ||
             homeAverage.PoissonProbability > 0.74 || awayAverage.PoissonProbability > 0.74) &&
            head2Head.MoreThanTwoGoals > SixthEightPercentage - 9)
        {
            return matchPrediction with
            {
                Probability = probability,
                Qualified = true
            };
        }

        return matchPrediction;
    }
    
    public static MatchPrediction NoGoalAnalysisBy(this Match home, Match away, Head2HeadAverage head2Head)
    {
        var homeAverage = home.HomeAverage;
        var homeOverallAverage = home.HomeOverallAverage;
        var awayAverage = away.AwayAverage;
        var awayOverallAverage = away.AwayOverallAverage;

        var zeroZeroProbability = CalculateAveragePercentage(
            homeAverage.ZeroZero,
            awayAverage.ZeroZero,
            homeOverallAverage.ZeroZero,
            awayOverallAverage.ZeroZero
        );
        
        var overTwoGoalsProbability = CalculateAveragePercentage(
            homeAverage.MoreThanTwoGoals,
            awayAverage.MoreThanTwoGoals,
            homeOverallAverage.MoreThanTwoGoals,
            awayOverallAverage.MoreThanTwoGoals
        );
        
        var goalGoalProbability = CalculateAveragePercentage(
            homeAverage.AtLeastOneGoal,
            awayAverage.AtLeastOneGoal,
            homeOverallAverage.AtLeastOneGoal,
            awayOverallAverage.AtLeastOneGoal
        );
        
        var matchPrediction = new MatchPrediction(false, zeroZeroProbability);
        
        if (homeAverage.ZeroZero + homeOverallAverage.ZeroZero > 15 &&
            awayAverage.ZeroZero + awayOverallAverage.ZeroZero > 15 &&
            head2Head.ZeroZero > SixthFivePercentage)
        {
            return matchPrediction with
            {
                Probability = zeroZeroProbability,
                Qualified = true
            };
        }
        if (homeAverage.PoissonProbability > 0.76 &&
            awayAverage.PoissonProbability > 0.76 &&
            head2Head.Count < 2)
        {
            return matchPrediction with
            {
                Probability = zeroZeroProbability,
                Qualified = true
            };
        }

        return matchPrediction;
    }
    
    public static MatchPrediction HomeWin(this Match home, Match away, Head2HeadAverage head2Head)
    {
        var homeAverage = home.HomeAverage;
        var homeOverallAverage = home.HomeOverallAverage;
        var awayAverage = away.AwayAverage;
        var awayOverallAverage = away.AwayOverallAverage;

        var probability = CalculateAveragePercentage(
            homeAverage.Win,
            homeOverallAverage.Win,
            homeAverage.PoissonProbability * 100,
            head2Head.HomWin
        );
        var matchPrediction = new MatchPrediction(false, probability);

        
        if ((probability > 60 && homeAverage.Win > 60 || homeOverallAverage.PoissonProbability > 0.70 && awayOverallAverage.PoissonProbability < 0.60 && homeOverallAverage.AtLeastOneGoal > 70) &&
            homeAverage.Win > awayAverage.Win &&
            (head2Head.Count > 2 && head2Head.HomWin > head2Head.AwayWin || true) &&
            homeAverage.PoissonProbability > awayAverage.PoissonProbability)
        {
            return matchPrediction with
            {
                Probability = probability,
                Qualified = true
            };
        }
        return matchPrediction;
    }
    
    public static MatchPrediction AwayWin(this Match home, Match away, Head2HeadAverage head2Head)
    {
        var homeAverage = home.HomeAverage;
        var homeOverallAverage = home.HomeOverallAverage;
        var awayAverage = away.AwayAverage;
        var awayOverallAverage = away.AwayOverallAverage;

        var probability = CalculateAveragePercentage(
            awayAverage.Win,
            awayOverallAverage.Win,
            awayAverage.PoissonProbability * 100,
            head2Head.AwayWin
        );
        var matchPrediction = new MatchPrediction(false, probability);
        
        if ((probability > 60 && awayAverage.Win > 60 || awayOverallAverage.PoissonProbability > 0.70 && homeOverallAverage.PoissonProbability < 0.60 && awayOverallAverage.AtLeastOneGoal > 70) &&
            awayAverage.Win > homeAverage.Win &&
            (head2Head.Count > 2 && head2Head.AwayWin > head2Head.HomWin || true) &&
            awayAverage.PoissonProbability > homeAverage.PoissonProbability)
        {
            return matchPrediction with
            {
                Probability = probability,
                Qualified = true
            };
        }
        return matchPrediction;
    }
    
    public static double CalculateAveragePercentage(this double? value1, double? value2, double? value3, double? value4)
    {
        var values = new List<double?> { value1, value2, value3, value4 };
        var nonNullValues = values.Where(v => v.HasValue).Select(v => v.Value);

        if (!nonNullValues.Any())
        {
            return 0.0;
        }

        return nonNullValues.Average();
    }
}
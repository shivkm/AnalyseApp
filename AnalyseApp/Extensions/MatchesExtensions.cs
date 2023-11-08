using AnalyseApp.Enums;
using AnalyseApp.models;

namespace AnalyseApp.Extensions;

internal static class MatchesExtensions
{
    private const double TwentyFactor =  0.20;
    private const double ThirtyFactor =  0.30;
    private const double Forty =  0.40;
    private const double Sixty =  0.60;
    
    internal static IEnumerable<Matches> OrderMatchesBy(this List<Matches> matches, DateTime playDate)
    {
        matches = matches.Where(i =>
            {
                var matchDate = i.Date.Parse();
                return matchDate < playDate;
            })
            .OrderByDescending(i => Convert.ToDateTime(i.Date))
            .ToList();

        return matches;
    }
    
    internal static IEnumerable<Matches> GetMatchesBy(this List<Matches> premierLeagueGames, string teamName, string playedOn)
    {
        var homeMatches = premierLeagueGames
            .Where(i =>
            {
                var matchDate = Convert.ToDateTime(i.Date);
                return matchDate < Convert.ToDateTime(playedOn);
            })
            .Where(i => i.HomeTeam == teamName || i.AwayTeam == teamName)
            .ToList();
        
        return homeMatches;
    } 
    
    internal static IEnumerable<Matches> GetCurrentLeagueGamesBy(this IList<Matches> matches, string teamName, int currentSeasonYear)
    {
        var formatStartDate = $"20/07/{currentSeasonYear}";
        var test = matches
            .Where(i => i.HomeTeam == teamName || i.AwayTeam == teamName);
        var foundMatches = matches
            .Where(i => i.HomeTeam == teamName || i.AwayTeam == teamName)
            .Where(i =>
            {
                var matchDate = i.Date.Parse();
                return matchDate > formatStartDate.Parse();
            })
            .OrderByDescending(i => i.Date.Parse())
            .ToList();

        return foundMatches;
    }

    public static Season GetSeason(this Matches match)
    {
        if (match.Date is null) return Season.Unknown;

        var matchDate = Convert.ToDateTime(match.Date);

        return matchDate.Month switch
        {
            >= 12 or <= 2 => Season.Winter,
            >= 3 and <= 5 => Season.Spring,
            >= 6 and <= 8 => Season.Summer,
            >= 9 and <= 11 => Season.Autumn,
            
        };
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
    
    
    internal static TeamAverage GetTeamAverageBy(this List<Matches> matches, string teamName)
    {
        var (scored, conceded, _) = matches.GetTeamScoredAndConcededGoalsBy(teamName);
        var scoreAvg = (double)scored.Sum() / conceded.Sum();
        
        var scoredGoalAtHome = matches.GetScoredGoalAvgBy(teamName);
        var concededGoalAtHome = matches.GetConcededGoalAvgBy(teamName);
        
        var scoredGoalAtAway = matches.GetScoredGoalAvgBy(teamName, true);
        var concededGoalAtAway = matches.GetConcededGoalAvgBy(teamName, true);

        var scoreAvgAtHome = 0.0;
        if (scoredGoalAtHome != 0)
        {
            scoreAvgAtHome = concededGoalAtHome == 0 
            ? 0.50 : scoredGoalAtHome / concededGoalAtHome;
            
        }

        var scoreAvgAtAway = 0.0;
        if (scoredGoalAtAway != 0)
        {
            scoreAvgAtAway = concededGoalAtAway == 0 
                ? 0.50 : scoredGoalAtAway / concededGoalAtAway;
        }
        
        
        // Calculate Exponential Moving Averages
        var scoredGoalsEma = CalculateExponentialMovingAverage(scored);
        var concededGoalsEma = CalculateExponentialMovingAverage(conceded);
        var exponentialMovingAvg = scoredGoalsEma / concededGoalsEma;
        
        // dispersion value calculation
        var dispersionValue = matches.CalculateDispersionScoreValueBy(teamName);
        
        var teamAvg = new TeamAverage(
            scoreAvg, 
            scoreAvgAtHome, 
            scoreAvgAtAway, 
            exponentialMovingAvg
        );
        
        return teamAvg;
    }
    
    /// <summary>
    /// Calculate scored goal average of the team
    /// </summary>
    /// <param name="matches">Team match list</param>
    /// <param name="teamName">Team name</param>
    /// <param name="atAway">Team play side</param>
    /// <returns>Scored goal average</returns>
    private static double GetScoredGoalAvgBy(this IEnumerable<Matches> matches, string teamName, bool atAway = false)
    {
        var teamGames = matches
            .Where(match => atAway ? match.AwayTeam == teamName : match.HomeTeam == teamName)
            .ToList();
        
        var teamScoredGoals = teamGames
            .Sum(match => atAway ? match.FTAG : match.FTHG)
            .GetValueOrDefault();
        
        var scoredGoalAvg = (double)teamScoredGoals / teamGames.Count;
        
        return scoredGoalAvg;
    }
    
    /// <summary>
    /// Calculate conceded goal average of the team
    /// </summary>
    /// <param name="matches">Team match list</param>
    /// <param name="teamName">Team name</param>
    /// <param name="atAway">Team play side</param>
    /// <returns>Conceded goal average</returns>
    private static double GetConcededGoalAvgBy(this IEnumerable<Matches> matches, string teamName, bool atAway = false)
    {
        var teamGames = matches
            .Where(match => atAway ? match.AwayTeam == teamName : match.HomeTeam == teamName)
            .ToList();
        
        var teamScoredGoals = teamGames
            .Sum(match => atAway ? match.FTHG : match.FTAG)
            .GetValueOrDefault();
        
        var scoredGoalAvg = (double)teamScoredGoals / teamGames.Count;
        
        return scoredGoalAvg;
    }

    /// <summary>
    /// Calculate based on the goals the dispersion value
    /// The Dispersion value represent the probability of score different 
    /// </summary>
    /// <param name="matches">List of The team matches</param>
    /// <param name="teamName">team name</param>
    /// <returns>Dispersion value</returns>
    private static double CalculateDispersionScoreValueBy(this IEnumerable<Matches> matches, string teamName)
    {
        var (_, _,totalGoals) = matches.GetTeamScoredAndConcededGoalsBy(teamName);
        var dispersionValue = CalculateDispersionValue(totalGoals);

        return dispersionValue;
    }

    private static double CalculateDispersionValue(IReadOnlyCollection<int> scores)
    {
        if (scores.Count == 0)
            return 0.0;
        var mean = scores.Average();
        var sumOfSquaredDeviations = scores.Sum(x => Math.Pow(x - mean, 2));
        var variance = sumOfSquaredDeviations / (scores.Count - 1);
        var standardDeviation = Math.Sqrt(variance);

        return standardDeviation;
    }

    /// <summary>
    /// Calculate the Weighted average of the team
    /// </summary>
    /// <param name="matches">List of The team matches</param>
    /// <param name="teamName">team name</param>
    /// <returns>Weighted average</returns>
    /// <exception cref="ArgumentException"></exception>
    internal static (double ScoredAvg, double ConcededAvg) CalculateWeightedAverage(this IEnumerable<Matches> matches, string teamName)
    {
        var weights = new List<double> { 0.3, 0.2, 0.2, 0.1, 0.1 };
        var (scores, conceded, totalGoals) = matches.GetTeamScoredAndConcededGoalsBy(teamName);
        
        if (scores.Count != weights.Count)
            throw new ArgumentException("The number of scores must match the number of weights.");

        double totalWeightedScore = 0;
        double totalWeightedConceded = 0;
        double totalWeight = 0;

        for (var i = 0; i < scores.Count; i++)
        {
            totalWeightedScore += scores[i] * weights[i];
            totalWeightedConceded += conceded[i] * weights[i];
            totalWeight += weights[i];
        }

        var weighedScoredAvg = totalWeightedScore / totalWeight;
        var weighedConcededAvg = totalWeightedConceded / totalWeight;

        return (weighedScoredAvg, weighedConcededAvg);
    }

    private static double CalculateExponentialMovingAverage(IReadOnlyList<int> goals)
    {
        if (goals.Count <= 0)
        {
            return 0.0;
        }
        const double weight = 0.2;
        double ema = goals[0]; 

        for (var i = 1; i < goals.Count; i++)
        {
            ema = weight * goals[i] + (1 - weight) * ema;
        }

        return ema;
    }
    
    internal static double GetGameAvgBy(this IEnumerable<Matches> matches, int count, Func<Matches, bool> predictor) =>
        matches.Count(predictor) / (double)count;
    
    private static (List<int> ScoredGoal, List<int> ConcededGoal, List<int> TotalGoals)  GetTeamScoredAndConcededGoalsBy(this IEnumerable<Matches> matches, string teamName)
    {
        var totalGoals = new List<int>();
        var scores = new List<int>();
        var conceded = new List<int>();
        foreach (var match in matches)
        {
            totalGoals.Add(match.FTHG.GetValueOrDefault() + match.FTAG.GetValueOrDefault());
            if (match.HomeTeam == teamName)
            {
                scores.Add(match.FTHG.GetValueOrDefault());
                conceded.Add(match.FTAG.GetValueOrDefault());
            }

            if (match.AwayTeam != teamName)
                continue;

            scores.Add(match.FTAG.GetValueOrDefault());
            conceded.Add(match.FTHG.GetValueOrDefault());
        }

        return (scores, conceded, totalGoals);
    }

    public static bool IsGoalsPossible(this double scoredProbability, double concededProbability)
    {
        const double fifty = 0.50;
        const double sixtyEight = 0.68;

        return scoredProbability > fifty && concededProbability > sixtyEight ||
               concededProbability > fifty && scoredProbability > fifty;
    }

    public static bool IsChaliGame(this TeamResult teamResult, TeamData teamData)
    {
        return teamResult.TwoToThreeGoals > 0.80 && 
               (teamResult.OverTwoGoals < 0.34 || teamResult.UnderTwoGoals < 0.34 || teamResult.BothScoredGoals < 0.34) &&
               teamData is { TeamScoredGames: >= 0.50, TeamConcededGoalGames: >= 0.66 };
    }
    
    public static bool IsScoredGame(this TeamData teamData)
    {
        return teamData is 
            { TeamScoredGames: >= 0.60, TeamConcededGoalGames: >= 0.60 } or
            {TeamScoredGames: >= 0.60, TeamConcededGoalGames: >= 0.50 } or
            {TeamScoredGames: >= 0.50, TeamConcededGoalGames: >= 0.60 }
            ;
    }
}
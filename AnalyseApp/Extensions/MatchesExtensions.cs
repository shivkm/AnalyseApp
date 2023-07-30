using AnalyseApp.models;

namespace AnalyseApp.Extensions;

internal static class MatchesExtensions
{
    /// <summary>
    /// Query last ten matches of the given team name
    /// </summary>
    /// <param name="matches">Historical matches</param>
    /// <param name="team">team name</param>
    /// <param name="lastMatchDateTime">last played match date</param>
    /// <returns>Filter Matches by team name and order by Date in descending order</returns>
    internal static List<Matches> GetLastTenGamesBy(this List<Matches> matches, string team, DateTime lastMatchDateTime)
    {
        matches = matches.Where(i =>
                    {
                        var matchDate = Convert.ToDateTime(i.Date);
                        return matchDate < lastMatchDateTime;
                    })
            .Where(item => item.HomeTeam == team || item.AwayTeam == team)
            .OrderByDescending(i => Convert.ToDateTime(i.Date))
            .Take(12)
            .ToList();
        
        return matches;
    }
    
    internal static List<Matches> GetCurrentSeasonGamesBy(this List<Matches> matches, string league, DateTime startDate, DateTime endDate)
    {
        matches = matches.Where(i =>
            {
                var matchDate = Convert.ToDateTime(i.Date);
                return matchDate >= startDate && matchDate <= endDate;
            })
            .Where(item => item.Div == league)
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
    
    internal static IEnumerable<Matches> GetSideMatchesBy(this List<Matches> premierLeagueGames, string teamName, bool isHome = true)
    {
        var homeMatches = premierLeagueGames
            .Where(i => isHome ? i.HomeTeam == teamName : i.AwayTeam == teamName)
            .ToList();
        
        return homeMatches;
    }

    internal static IEnumerable<Matches> GetHeadToHeadMatchesBy(this List<Matches> premierLeagueGames, string homeTeam, string awayTeam, string playedOn)
    {
        var homeMatches = premierLeagueGames
            .Where(i =>
            {
                var matchDate = Convert.ToDateTime(i.Date);
                return matchDate < Convert.ToDateTime(playedOn);
            })
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                        i.AwayTeam == homeTeam && i.HomeTeam == awayTeam)
            .ToList();
        
        return homeMatches;
    }
    
    internal static double GetScoredGoalAverageBy(this List<Matches> matches, string teamName)
    {
        var homeSideMatches = matches.Where(ii => ii.HomeTeam == teamName);
        var homeSideGoals = homeSideMatches.Sum(i => i.FTHG);
        
        var awaySideMatches = matches.Where(ii => ii.AwayTeam == teamName);
        var awaySideGoals = awaySideMatches.Sum(i => i.FTAG);
        var value = (double)(homeSideGoals + awaySideGoals) / matches.Count;

        return value;
    }
    
    internal static double GetConcededGoalAverageBy(this List<Matches> matches, string teamName)
    {
        var homeSideGoals = matches
            .Where(ii => ii.HomeTeam == teamName)
            .Sum(i => i.FTAG);
        
        var awaySideGoals = matches
            .Where(ii => ii.AwayTeam == teamName)
            .Sum(i => i.FTHG);
        
        var value = (double)(homeSideGoals + awaySideGoals) / matches.Count;

        return value;
    }
    
    internal static double GetAwayGoalScoredAverageBy(this List<Matches> matches, string teamName)
    {
        var awaySideMatches = matches.Where(ii => ii.AwayTeam == teamName);
        var awaySideGoals = awaySideMatches.Sum(i => i.FTAG);
        
        var value = (double)awaySideGoals / matches.Count;

        return value;
    }
    
    internal static double GetHomeGoalScoredAverageBy(this List<Matches> matches, string teamName)
    {
        var homeSideMatches = matches.Where(ii => ii.HomeTeam == teamName);
        var homeSideGoals = homeSideMatches.Sum(i => i.FTHG);
        
        var value = (double)homeSideGoals / matches.Count;

        return value;
    }
       
    internal static double GetHomeGoalConcededAverageBy(this List<Matches> matches, string teamName)
    {
        var homeSideMatches = matches.Where(ii => ii.HomeTeam == teamName);
        var homeSideGoals = homeSideMatches.Sum(i => i.FTAG);
       
        var value = (double)homeSideGoals / matches.Count;

        return value;
    }
       
    internal static double GetAwayGoalConcededAverageBy(this List<Matches> matches, string teamName)
    {
        var homeSideMatches = matches.Where(ii => ii.AwayTeam == teamName);
        var homeSideGoals = homeSideMatches.Sum(i => i.FTHG);
       
        var value = (double)homeSideGoals / matches.Count;

        return value;
    }
    
    /// <summary>
    /// Query head to head matches
    /// </summary>
    /// <param name="matches">Historical matches</param>
    /// <param name="homeTeam"></param>
    /// <param name="awayTeam"></param>
    /// <returns>Filter Matches by team name and order by Date in descending order</returns>
    internal static List<Matches> GetHeadToHeadGamesBy(this List<Matches> matches, string homeTeam, string awayTeam)
    {
        matches = matches
            .Where(item => (item.HomeTeam == homeTeam && item.AwayTeam == awayTeam) ||
                                  (item.HomeTeam == awayTeam && item.AwayTeam == homeTeam))
            .OrderByDescending(i => Convert.ToDateTime(i.Date))
            .Take(12)
            .ToList();
        
        return matches;
    }

    /// <summary>
    /// Calculate Team statistic
    /// </summary>
    /// <param name="historicalMatches">Filter matches</param>
    /// <param name="team">Team name</param>
    internal static TeamStatistic GetTeamStatistics(this List<Matches> historicalMatches, string team)
    {
        var homeMatches = historicalMatches
            .Where(item => item.HomeTeam == team)
            .ToList();

        var awayMatches = historicalMatches
            .Where(item => item.AwayTeam == team)
            .ToList();

        var totalMatches = new List<Matches>();
        totalMatches.AddRange(homeMatches);
        totalMatches.AddRange(awayMatches);
        
        var scoreAvg = GetScoreAverages(team, totalMatches);
        var homeScoreProbability = GetGoalProbabilities(scoreAvg.homeAvg);
        var awayScoreProbability = GetGoalProbabilities(scoreAvg.awayAvg);

        var homeProbability = homeScoreProbability.First(ii => ii.Key is 1).Value;
        var awayProbability = awayScoreProbability.First(ii => ii.Key is 1).Value;
        var scored =  homeMatches.GetScoreGameAvg(awayMatches);
        var concededScored =  homeMatches.GetConcededScoredGameAvg(awayMatches);
        var oneSidedConceded = homeMatches.GetConcededOneSidedGoalsGameAvg(awayMatches);
        var oneSidedScored = homeMatches.GetScoredOneSidedGoalsGameAvg(awayMatches);
        var zeroZeroScored = homeMatches.GetZeroZeroGoalsGameAvg(awayMatches);
        var twoToThreeGoalScored = homeMatches.GetTwoToThreeGoalScoredAvg(awayMatches);
        var overTwoGoalsScored = homeMatches.GetOverTwoGoalsGameAvg(awayMatches);
        var overThreeGoalScored = homeMatches.GetOverThreeGoalsGameAvg(awayMatches);
        var bothTeamScoredGoal = homeMatches.GetBothTeamScoredGameAvg(awayMatches);


        var teamStatistics = new TeamStatistic(
            scored,
            concededScored,
            oneSidedScored,
            oneSidedConceded,
            zeroZeroScored,
            twoToThreeGoalScored,
            overTwoGoalsScored,
            overThreeGoalScored,
            bothTeamScoredGoal,
            homeProbability,
            awayProbability
        );

        return teamStatistics;
    }

    /// <summary>
    /// Calculate Team statistic
    /// </summary>
    /// <param name="historicalMatches">Filter matches</param>
    /// <param name="homeTeam"></param>
    /// <param name="awayTeam"></param>
    internal static TeamStatistic GetHeadToHeadStatistics(
        this List<Matches> historicalMatches, string homeTeam, string awayTeam)
    {
        var headToHead = historicalMatches
            .Where(item => (item.HomeTeam == homeTeam && item.AwayTeam == awayTeam) ||
                           (item.HomeTeam == awayTeam && item.AwayTeam == homeTeam))
            .ToList();

        var scored =  headToHead.GetScoreGameAvg(headToHead);
        var oneSidedConceded = headToHead.GetConcededOneSidedGoalsGameAvg(headToHead);
        var oneSidedScored = headToHead.GetScoredOneSidedGoalsGameAvg(headToHead);
        var zeroZeroScored = headToHead.GetZeroZeroGoalsGameAvg(headToHead);
        var twoToThreeGoalScored = headToHead.GetTwoToThreeGoalScoredAvg(headToHead);
        var overTwoGoalsScored = headToHead.GetOverTwoGoalsGameAvg(headToHead);
        var overThreeGoalScored = headToHead.GetOverThreeGoalsGameAvg(headToHead);
        var bothTeamScoredGoal = headToHead.GetBothTeamScoredGameAvg(headToHead);

        var teamStatistics = new TeamStatistic(
            scored,
            default,
            oneSidedScored,
            oneSidedConceded,
            zeroZeroScored,
            twoToThreeGoalScored,
            overTwoGoalsScored,
            overThreeGoalScored,
            bothTeamScoredGoal,
            default,
            default
        );

        return teamStatistics;
    }
    
    /// <summary>
    /// Calculate the avg of team scored in given matches
    /// IMPORTANT: Calculate only teams score not the competitors scores
    /// </summary>
    /// <param name="homeMatches"></param>
    /// <param name="awayMatches"></param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetScoreGameAvg(this ICollection<Matches> homeMatches, ICollection<Matches> awayMatches)
    {
        var homeScoreAvg = (double)homeMatches.Count(i => i.FTHG > 0) / homeMatches.Count;
        var awayScoreAvg = (double)awayMatches.Count(i => i.FTAG > 0) / awayMatches.Count;
        
        var avg = new Average(homeScoreAvg, awayScoreAvg, default);
        return avg;
    }
    
    /// <summary>
    /// Calculate the avg of team scored in given matches
    /// IMPORTANT: Calculate only teams score not the competitors scores
    /// </summary>
    /// <param name="homeMatches"></param>
    /// <param name="awayMatches"></param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetConcededScoredGameAvg(this ICollection<Matches> homeMatches, ICollection<Matches> awayMatches)
    {
        var homeScoreAvg = (double)homeMatches.Count(i => i.FTAG > 0) / homeMatches.Count;
        var awayScoreAvg = (double)awayMatches.Count(i => i.FTHG > 0) / awayMatches.Count;
        
        var avg = new Average(homeScoreAvg, awayScoreAvg, default);
        return avg;
    }
    
    /// <summary>
    /// Calculate the average of conceded one sided goal games
    /// </summary>
    /// <param name="homeMatches"></param>
    /// <param name="awayMatches"></param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetConcededOneSidedGoalsGameAvg(this ICollection<Matches> homeMatches, ICollection<Matches> awayMatches)
    {
        var homeScoreAvg = (double)homeMatches.Count(i => i is { FTHG: 0, FTAG: > 0 }) / homeMatches.Count;
        var awayScoreAvg = (double)awayMatches.Count(i => i is { FTHG: > 0, FTAG: 0 }) / awayMatches.Count;
        
        var avg = new Average(homeScoreAvg, awayScoreAvg, default);
        return avg;
    }
    
    /// <summary>
    /// Calculate the average of scored one sided goal games
    /// </summary>
    /// <param name="homeMatches"></param>
    /// <param name="awayMatches"></param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetScoredOneSidedGoalsGameAvg(this ICollection<Matches> homeMatches, ICollection<Matches> awayMatches)
    {
        var homeScoreAvg = (double)homeMatches.Count(i => i is { FTHG: > 0, FTAG: 0 }) / homeMatches.Count;
        var awayScoreAvg = (double)awayMatches.Count(i => i is { FTHG: 0, FTAG: > 0 }) / awayMatches.Count;
        
        var avg = new Average(homeScoreAvg, awayScoreAvg, default);
        return avg;
    }
    
    
    /// <summary>
    /// Calculate the average of zero zero goal games
    /// </summary>
    /// <param name="homeMatches"></param>
    /// <param name="awayMatches"></param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetZeroZeroGoalsGameAvg(this ICollection<Matches> homeMatches, ICollection<Matches> awayMatches)
    {
        var homeScoreAvg = (double)homeMatches.Count(i => i is { FTHG: 0, FTAG: 0 }) / homeMatches.Count;
        var awayScoreAvg = (double)awayMatches.Count(i => i is { FTHG: 0, FTAG: 0 }) / awayMatches.Count;
        
        var avg = new Average(homeScoreAvg, awayScoreAvg, default);
        return avg;
    }


    /// <summary>
    /// Calculate the average of team scored over three goals
    /// </summary>
    /// <param name="homeMatches"></param>
    /// <param name="awayMatches"></param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetTwoToThreeGoalScoredAvg(this ICollection<Matches> homeMatches, ICollection<Matches> awayMatches)
    {
        var homeScoreAvg = (double)homeMatches.Count(i => i.FTHG + i.FTAG is 2 or 3) / homeMatches.Count;
        var awayScoreAvg = (double)awayMatches.Count(i => i.FTHG + i.FTAG is 2 or 3) / awayMatches.Count;

        var avg = new Average(homeScoreAvg, awayScoreAvg, default); 
        return avg;
    }

    /// <summary>
    /// Calculate the average of team scored over two goals
    /// </summary>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetOverTwoGoalsGameAvg(this ICollection<Matches> homeMatches, ICollection<Matches> awayMatches)
    {
        var homeScoreAvg = (double)homeMatches.Count(i => i.FTHG + i.FTAG > 2) / homeMatches.Count;
        var awayScoreAvg = (double)awayMatches.Count(i => i.FTHG + i.FTAG > 2) / awayMatches.Count;

        var avg = new Average(homeScoreAvg, awayScoreAvg, default);
        return avg;
    }

    /// <summary>
    /// Calculate the average of team scored two to three goals
    /// </summary>
    /// <param name="homeMatches"></param>
    /// <param name="awayMatches"></param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetOverThreeGoalsGameAvg(this ICollection<Matches> homeMatches, ICollection<Matches> awayMatches)
    {
        var homeScoreAvg = (double)homeMatches.Count(i => i.FTHG + i.FTAG > 3) / homeMatches.Count;
        var awayScoreAvg = (double)awayMatches.Count(i => i.FTHG + i.FTAG > 3) / awayMatches.Count;

        var avg = new Average(homeScoreAvg, awayScoreAvg, default); 
        return avg;
    }


    /// <summary>
    /// Calculate the average of team scored over three goals
    /// </summary>
    /// <param name="homeMatches"></param>
    /// <param name="awayMatches"></param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetBothTeamScoredGameAvg(this ICollection<Matches> homeMatches, ICollection<Matches> awayMatches)
    {
        var homeScoreAvg = (double)homeMatches.Count(i => i is { FTHG: > 0, FTAG: > 0 }) / homeMatches.Count;
        var awayScoreAvg = (double)awayMatches.Count(i => i is { FTHG: > 0, FTAG: > 0 }) / awayMatches.Count;

        var avg = new Average(homeScoreAvg, awayScoreAvg, default); 
        return avg;
    }


    
    /// <summary>
    /// Calculate the average of both team scored goals
    /// </summary>
    /// <param name="historicalMatches">Filter matches of the team</param>
    /// <param name="team">Team name</param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetBothTeamScoredGoalGameAvg(this ICollection<Matches> historicalMatches, string team)
    {
        var homeMatches = historicalMatches
            .Count(item => item.HomeTeam == team && item is { FTHG: > 0, FTAG: > 0 });

        var awayMatches = historicalMatches
            .Count(item => item.AwayTeam == team && item is { FTAG: > 0, FTHG: > 0 });

        var homeScoreAvg = (double)homeMatches / historicalMatches.Count;
        var awayScoreAvg = (double)awayMatches / historicalMatches.Count;

        var lastThreeMatches = historicalMatches.Take(3).ToList();

        var lastThreeResult = string.Join(", ", lastThreeMatches
            .Select(m => $"{m.FTHG}:{m.FTAG}")
            .ToArray());
        var scored = lastThreeMatches
            .Count(i => i.HomeTeam == team && i is { FTHG: > 0, FTAG: > 0 } ||
                               i.AwayTeam == team && i is { FTHG: > 0, FTAG: > 0 });
        
        var avg = new Average(homeScoreAvg, awayScoreAvg, scored, lastThreeResult);
        return avg;
    }
    
    private static (double homeAvg, double awayAvg) GetScoreAverages(string teamName, IList<Matches> matches)
    {
        int homeGoals = 0, homeMatches = 0, awayGoals = 0, awayMatches = 0;
    
        foreach (var match in matches)
        {
            if (match.HomeTeam == teamName)
            {
                homeGoals += match.FTHG ?? 0;
                homeMatches++;
            }
            else if (match.AwayTeam == teamName)
            {
                awayGoals += match.FTAG ?? 0;
                awayMatches++;
            }
        }
    
        var homeAvg = homeMatches > 0 ? (double)homeGoals / homeMatches : 0;
        var awayAvg = awayMatches > 0 ? (double)awayGoals / awayMatches : 0;
    
        return (homeAvg, awayAvg);
    }
    
    private static double Poisson(int goals, double avgGoals)
    {
        return Math.Pow(avgGoals, goals) * Math.Exp(-avgGoals) / Factorial(goals);
    }

    private static int Factorial(int n)
    {
        if (n <= 1) return 1;
        return n * Factorial(n - 1);
    }

    private static Dictionary<int, double> GetGoalProbabilities(double avgGoals)
    {
        var goalProbabilities = new Dictionary<int, double>();
        for (int i = 0; i <= 10; i++)
        {
            goalProbabilities.Add(i, Poisson(i, avgGoals));
        }
        return goalProbabilities;
    }
}
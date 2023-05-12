using AnalyseApp.models;

namespace AnalyseApp.Extensions;

internal static class MatchesExtensions
{
    /// <summary>
    /// Query last ten matches of the given team name
    /// </summary>
    /// <param name="matches">Historical matches</param>
    /// <param name="team">team name</param>
    /// <returns>Filter Matches by team name and order by Date in descending order</returns>
    internal static List<Matches> GetLastTenGamesBy(this List<Matches> matches, string team)
    {
        matches = matches
            .Where(item => item.HomeTeam == team || item.AwayTeam == team)
            .OrderByDescending(i => Convert.ToDateTime(i.Date))
            .Take(10)
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
        var scoreAvg =  historicalMatches.GetScoreGameAvg(team);
        var overTwoGoals = historicalMatches.GetOverTwoGoalsGameAvg(team);
        var twoToThreeGoals = historicalMatches.GetTwoToThreeGoalsGameAvg(team);
        var bothTeamScoreGoals = historicalMatches.GetBothTeamScoredGoalGameAvg(team);
        var oneSidedGoals = historicalMatches.GetOneSidedGoalsGameAvg(team);


        var teamStatistics = new TeamStatistic(
            scoreAvg,
            overTwoGoals,
            bothTeamScoreGoals,
            twoToThreeGoals,
            oneSidedGoals,
            oneSidedGoals,
            oneSidedGoals
        );

        return teamStatistics;
    }

    /// <summary>
    /// Calculate the avg of team scored in given matches
    /// IMPORTANT: Calculate only teams score not the competitors scores
    /// </summary>
    /// <param name="historicalMatches">Filter matches of the team</param>
    /// <param name="team">Team name</param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetScoreGameAvg(this ICollection<Matches> historicalMatches, string team)
    {
        var homeMatches = historicalMatches
            .Count(item => item.HomeTeam == team && item.FTHG > 0);

        var awayMatches = historicalMatches
            .Count(item => item.AwayTeam == team && item.FTAG > 0);

        var homeScoreAvg = (decimal)homeMatches / historicalMatches.Count;
        var awayScoreAvg = (decimal)awayMatches / historicalMatches.Count;

        var lastThreeMatches = historicalMatches.Take(3).ToList();
        var lastThreeResult = string.Join(", ", lastThreeMatches
            .Select(m => $"{m.FTHG}:{m.FTAG}")
            .ToArray());
        
        var scored = lastThreeMatches
            .Count(i => i.HomeTeam == team && i.FTHG > 0 || i.AwayTeam == team && i.FTAG > 0);
        
        var avg = new Average(homeScoreAvg, awayScoreAvg, scored, lastThreeResult);
        return avg;
    }
    
    /// <summary>
    /// Calculate the average of team scored over two goals
    /// </summary>
    /// <param name="historicalMatches">Filter matches of the team</param>
    /// <param name="team">Team name</param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetOverTwoGoalsGameAvg(this ICollection<Matches> historicalMatches, string team)
    {
        var homeMatches = historicalMatches
            .Count(item => item.HomeTeam == team && item.FTHG + item.FTAG > 2);

        var awayMatches = historicalMatches
            .Count(item => item.AwayTeam == team && item.FTHG + item.FTAG > 2);

        var homeScoreAvg = (decimal)homeMatches / historicalMatches.Count;
        var awayScoreAvg = (decimal)awayMatches / historicalMatches.Count;

        var lastThreeMatches = historicalMatches.Take(3).ToList();
        var lastThreeResult = string.Join(", ", lastThreeMatches
            .Select(m => $"{m.FTHG}:{m.FTAG}")
            .ToArray());
        var scored = lastThreeMatches
            .Count(i => i.HomeTeam == team && i.FTHG + i.FTAG > 2 || i.AwayTeam == team && i.FTHG + i.FTAG > 2);

        var avg = new Average(homeScoreAvg, awayScoreAvg, scored, lastThreeResult);
        return avg;
    }
    
    /// <summary>
    /// Calculate the average of team scored two to three goals
    /// </summary>
    /// <param name="historicalMatches">Filter matches of the team</param>
    /// <param name="team">Team name</param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetTwoToThreeGoalsGameAvg(this ICollection<Matches> historicalMatches, string team)
    {
        var homeMatches = historicalMatches
            .Count(item => item.HomeTeam == team && (item.FTHG + item.FTAG >= 2 && item.FTHG + item.FTAG <= 3));

        var awayMatches = historicalMatches
            .Count(item => item.AwayTeam == team && (item.FTHG + item.FTAG >= 2 && item.FTHG + item.FTAG <= 3));

        var homeScoreAvg = (decimal)homeMatches / historicalMatches.Count;
        var awayScoreAvg = (decimal)awayMatches / historicalMatches.Count;

        var lastThreeMatches = historicalMatches.Take(3).ToList();
        
        var lastThreeResult = string.Join(", ", lastThreeMatches
            .Select(m => $"{m.FTHG}:{m.FTAG}")
            .ToArray());
        var scored = lastThreeMatches
            .Count(i => i.HomeTeam == team && (i.FTHG + i.FTAG >= 2 && i.FTHG + i.FTAG <= 3) ||
                               i.AwayTeam == team && (i.FTHG + i.FTAG >= 2 && i.FTHG + i.FTAG <= 3));
        
        var avg = new Average(homeScoreAvg, awayScoreAvg, scored, lastThreeResult); 
        return avg;
    }
    
    /// <summary>
    /// Calculate the average of team not scored goals
    /// </summary>
    /// <param name="historicalMatches">Filter matches of the team</param>
    /// <param name="team">Team name</param>
    /// <returns>Calculated Average of team in both fields Home and Away Side</returns>
    private static Average GetOneSidedGoalsGameAvg(this ICollection<Matches> historicalMatches, string team)
    {
        var homeMatches = historicalMatches
            .Count(item => item.HomeTeam == team && item is { FTHG: 0, FTAG: > 0 });

        var awayMatches = historicalMatches
            .Count(item => item.AwayTeam == team && item is { FTAG: 0, FTHG: > 0 });

        var homeScoreAvg = (decimal)homeMatches / historicalMatches.Count;
        var awayScoreAvg = (decimal)awayMatches / historicalMatches.Count;
        
        var lastThreeMatches = historicalMatches.Take(3).ToList();
        
        var lastThreeResult = string.Join(", ", lastThreeMatches
            .Select(m => $"{m.FTHG}:{m.FTAG}")
            .ToArray());
        var scored = lastThreeMatches
            .Count(i => i.HomeTeam == team && i is { FTHG: 0, FTAG: > 0 } ||
                                i.AwayTeam == team && i is { FTHG: 0, FTAG: > 0 });
        var avg = new Average(homeScoreAvg, awayScoreAvg, scored, lastThreeResult);
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

        var homeScoreAvg = (decimal)homeMatches / historicalMatches.Count;
        var awayScoreAvg = (decimal)awayMatches / historicalMatches.Count;

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
}
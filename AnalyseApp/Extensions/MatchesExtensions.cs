using AnalyseApp.models;

namespace AnalyseApp.Extensions;

internal static class MatchesExtensions
{
    /// <summary>
    /// Filter the matches for current season
    /// </summary>
    /// <param name="matches">Historical matches</param>
    /// <returns>Matches from current season</returns>
    internal static IList<Matches> GetCurrentSeasonBy(this IEnumerable<Matches> matches)
    {
        var filterMatches = matches
            .Where(g => DateTime.Parse(g.Date).Year == DateTime.Now.AddYears(-1).Year || 
                                DateTime.Parse(g.Date).Year == DateTime.Now.Year)
            .ToList();

        return filterMatches;
    }

    internal static void GenerateOutput(this NextMatch nextMatch)
    {
        Console.WriteLine($"Qualified matches {nextMatch}\t\n");
    }

    internal static void FindTopFiveGamesBy(this IList<NextMatch> nextMatches, double percentage)
    {
        if (!nextMatches.Any())
            return;

        var nextMatchesWithBothScore = new List<NextMatch>();
        var matchesBothScore = nextMatches
            .Where(i => i.LastSixSeason?.HomeTeam?.OneGoal > percentage &&
                                i.LastSixSeason.AwayTeam?.OneGoal > percentage);
        
        nextMatchesWithBothScore.AddRange(matchesBothScore);
        
        var nextMatchesWithGoalInFirstHalf = new List<NextMatch>();
        var matchesWithGoalInFirstHalf = nextMatches
            .Where(i => i.LastSixSeason?.HomeTeam?.HalfTimeWithOneGoal > percentage &&
                                i.LastSixSeason.AwayTeam?.HalfTimeWithOneGoal > percentage);
        
        nextMatchesWithGoalInFirstHalf.AddRange(matchesWithGoalInFirstHalf);
        
        var nextMatchesWithTwoGoals = new List<NextMatch>();
        var matchesWithTwoGoals = nextMatches
            .Where(i => i.LastSixSeason?.HomeTeam?.TwoGoals > percentage &&
                        i.LastSixSeason.AwayTeam?.TwoGoals > percentage);
        
        nextMatchesWithTwoGoals.AddRange(matchesWithTwoGoals);
        
        var nextMatchesWithZeroZero = new List<NextMatch>();
        var matchesWithZeroZero = nextMatches
            .Where(i => i.LastSixSeason?.HomeTeam?.ZeroZero < 10 &&
                        i.LastSixSeason.AwayTeam?.ZeroZero < 10);
        
        nextMatchesWithZeroZero.AddRange(matchesWithZeroZero);
        
        var nextMatchesWithTwoToThree = new List<NextMatch>();
        var matchesWithTwoToThree = nextMatches
            .Where(i => i.LastSixSeason?.HomeTeam?.ZeroZero < 10 &&
                        i.LastSixSeason.AwayTeam?.ZeroZero < 10);
        
        nextMatchesWithTwoToThree.AddRange(matchesWithTwoToThree);
        
        // Now find in all list the common game which could be the best candidate for bet

        var commonGamesWithGoalsInBothTimes = 
            nextMatchesWithBothScore.Where(i => nextMatchesWithGoalInFirstHalf.Contains(i));
        
        var commonGamesWithGoalsInBothTimeAtMoreThanTwoGoals =
            commonGamesWithGoalsInBothTimes.Where(i => nextMatchesWithTwoGoals.Contains(i));

        var commonGamesWithGoalsAndLessZeroZero = 
            commonGamesWithGoalsInBothTimeAtMoreThanTwoGoals.Where(i => nextMatchesWithZeroZero.Contains(i))
                .ToList();

        if (commonGamesWithGoalsAndLessZeroZero.Any())
        {
            Console.WriteLine("################ -- Qualified matches for all three options 'Both Team Score' or 'More Than 2 Goals' or 'At least one Goal in first half'  -- ################");
            commonGamesWithGoalsAndLessZeroZero.ForEach(i => Console.Write("{0}\t", i));
            return;

        }

        var commonGamesWithZeroZero = nextMatchesWithTwoToThree.Where(i => nextMatchesWithZeroZero.Contains(i))
            .ToList();
        
        if (commonGamesWithZeroZero.Any())
        {
            Console.WriteLine("################ -- Qualified matches for 'Two to three goals'   -- ################");
            commonGamesWithZeroZero.ForEach(i => Console.Write("{0}\t", i));
            return;
        }

        if (nextMatchesWithBothScore.Any())
        {
            if (!nextMatchesWithBothScore.Any(i => nextMatchesWithZeroZero.Contains(i)))
                return;
            
            Console.WriteLine("################ -- Qualified matches for 'Both Team Score'   -- ################");
            nextMatchesWithBothScore.ForEach(i => Console.Write("{0}\t", i));
            return;
        }

        if (!nextMatchesWithTwoGoals.Any(i => commonGamesWithZeroZero.Contains(i)))
            return;
        
        Console.WriteLine("################ -- Qualified matches for 'More Than two Goals'   -- ################");
        nextMatchesWithTwoGoals.ForEach(i => Console.Write("{0}\t", i));
        
        
        

    }


    /// <summary>
    /// This will filter the matches by team name and analyse following points
    ///     * at least one goal overall and at home or away depend on where the team is playing
    ///     * two or more goals overall and at home or away depend on where the team is playing
    ///     * Two to three goals overall and at home or away depend on where the team is playing
    ///     * zero zero games overall and at home or away depend on where the team is playing
    /// </summary>
    /// <param name="matches">Historical or current season matches</param>
    /// <param name="team">current team to analyse</param>
    /// <param name="isHome">if the analysing team is on home field</param>
    /// <param name="overAll">analyse the game without specifying the field</param>
    /// <param name="passingPercentage">passing percentage to avoid bad performing games
    ///         * For at least one goal in full time should be bigger than given percentage
    ///         * For at least one goal in halftime should be bigger than given percentage - 20
    ///         * For 0:0 game average should below then given percentage - 35
    ///         * For more than two goals should be bigger than given percentage - 10</param>
    /// <param name="currentSeasonPercentage"></param>
    /// <returns>If the zero zero result is less than 10 and at least one goal pass the passing percentage</returns>
    internal static GameAverage? TeamPerformance(
        this IList<Matches> matches,
        string team,
        bool isHome,
        bool overAll,
        int passingPercentage,
        int currentSeasonPercentage
        
    )
    {
        var result = new GameAverage();
        var teamMatches = overAll ?
            matches.GetMatchesBy(a => a.HomeTeam == team || a.AwayTeam == team):
            matches.GetMatchesBy(a => isHome ? a.HomeTeam == team : a.AwayTeam == team);

        // At least over 50% should make a goal in full time
        result.OneGoal = teamMatches.GoalsInFullTime(team, isHome, overAll, 1);
        if (result.OneGoal < passingPercentage || 
            currentSeasonPercentage != 0 && result.OneGoal < currentSeasonPercentage)
            return null;
        
        // Two goals over 45% in full time
        result.TwoGoals = teamMatches.GoalsInFullTime(team, isHome, overAll, 2);
        if (result.TwoGoals < passingPercentage - 10 ||
            currentSeasonPercentage != 0 && result.TwoGoals < currentSeasonPercentage)
            return null;
        
        // At least over 25% should make a gaol in halftime
        result.HalfTimeWithOneGoal = teamMatches.GoalsInFirstHalf(isHome, overAll);
        if (result.HalfTimeWithOneGoal < passingPercentage - 20 &&
            currentSeasonPercentage != 0 && result.HalfTimeWithOneGoal < currentSeasonPercentage)
            return null;
        
        // 0:0 games are less than 10% 
        result.ZeroZero = teamMatches.ZeroZeroGoal();
        if(result.ZeroZero > passingPercentage - 30 &&
           currentSeasonPercentage != 0 && result.OneGoal < currentSeasonPercentage)
           return null;
        
        result.TwoToThree = teamMatches.TwoToThreeGoals();
        return result;
    }
    
    /// <summary>
    /// This will filter the matches where home and away team played and analyse following points
    ///     * at least one goal overall and at home or away depend on where the team is playing
    ///     * two or more goals overall and at home or away depend on where the team is playing
    ///     * Two to three goals overall and at home or away depend on where the team is playing
    ///     * zero zero games overall and at home or away depend on where the team is playing
    /// </summary>
    /// <param name="matches">Historical or current season matches</param>
    /// <param name="homeTeam">Home team</param>
    /// <param name="awayTeam">Away team</param>
    /// <returns></returns>
    internal static Head2HeadAverage AnalyseHeadToHeadPerformance(
        this IList<Matches> matches, 
        string homeTeam,
        string awayTeam
    )
    {
        var atHomeMatches = matches.GetMatchesBy(a => a.HomeTeam == homeTeam && a.AwayTeam == awayTeam);

        var result = new Head2HeadAverage
        {
            ZeroZero = atHomeMatches.ZeroZeroGoal(),
            BothTeamScore = atHomeMatches.BothTeamMakeGoal(),
            MoreThanTwoGoals = atHomeMatches.MoreThanTwoGoals(),
            TwoToThree = atHomeMatches.TwoToThreeGoals(),
            GoalInFirstHalf = atHomeMatches.GoalsInFirstHalf(false, true),
            Hint = atHomeMatches.Count > 5 ? "" : "Not enough data for analysis!"
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
    
    private static double ZeroZeroGoal(this IEnumerable<Matches> matches) =>
        matches.Percent(p =>  p is { FTHG: 0, FTAG: 0 });

    private static double BothTeamMakeGoal(this IEnumerable<Matches> matches) =>
        matches.Percent(a => a is { FTHG: > 0, FTAG: > 0 });
    
    private static double GoalsInFirstHalf(this IEnumerable<Matches> matches, bool isHome, bool overAll)
    {
        return overAll 
            ? matches.Percent(a => a.HTHG > 0 || a.HTAG > 0)
            : matches.Percent(a => isHome ? a.HTHG > 0 : a.HTAG > 0);
    }
    
    private static double GoalsInFullTime(
        this IEnumerable<Matches> matches,
        string team,
        bool isHome, 
        bool overall, 
        int expectedGoal
        )
    {
        return overall 
        ? matches.Percent(a => (a.HomeTeam == team || a.AwayTeam == team) && a.FTHG + a.FTAG >= expectedGoal)
        : matches.Percent(a => isHome ? a.HomeTeam == team && a.FTHG > expectedGoal : a.AwayTeam == team && a.FTAG > expectedGoal);
    }
    
    private static double MoreThanTwoGoals(this IEnumerable<Matches> matches) =>
        matches.Percent(a =>  a.FTHG + a.FTAG >= 3);
    
    private static double TwoToThreeGoals(this IEnumerable<Matches> matches) =>
        matches.Percent(a => a.FTHG + a.FTAG <= 3 && a.FTHG + a.FTAG > 1);
    
    private static double GetPercentageOfTeamWithExpectedGoal(
        this IList<Matches> matches,
        bool isHome,
        int expectedGoal
    )
    {
        var result = matches.Percent(p => isHome ? p.FTHG >= expectedGoal : p.FTAG >= expectedGoal);

        return result;
    }
    
    private static double Percent<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var total = 0;
        var count = 0;

        var enumerable = source.ToList();
        if (!enumerable.Any())
            return 0;

        foreach (T item in enumerable)
        {
            ++count;
            if (predicate(item))
            {
                total += 1;
            }
        }

        return 100.0 * total / count;
    }

}
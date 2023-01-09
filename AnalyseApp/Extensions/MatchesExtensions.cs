using System.Text.Json;
using AnalyseApp.models;

namespace AnalyseApp.Extensions;

internal static class MatchesExtensions
{
    /// <summary>
    /// Filter the matches for current season
    /// </summary>
    /// <param name="matches">Historical matches</param>
    /// <returns>Matches from current season</returns>
    internal static IList<GameData> GetCurrentSeasonBy(this IEnumerable<GameData> matches)
    {
        var filterMatches = matches
            .Where(g => DateTime.Parse(g.Date).Year == DateTime.Now.AddYears(-1).Year || 
                                DateTime.Parse(g.Date).Year == DateTime.Now.Year)
            .ToList();

        return filterMatches;
    }

    internal static void FindTopFiveGamesBy(this IList<NextMatch2> nextMatches, double percentage)
    {
        if (!nextMatches.Any())
            return;

        var nextMatchesWithBothScore = new List<NextMatch2>();
        var matchesBothScore = nextMatches
            .Where(i => i.LastSixSeason?.HomeTeam?.OneGoal2 > percentage &&
                                i.LastSixSeason.AwayTeam?.OneGoal2 > percentage);
        
        nextMatchesWithBothScore.AddRange(matchesBothScore);
        
        var nextMatchesWithGoalInFirstHalf = new List<NextMatch2>();
        var matchesWithGoalInFirstHalf = nextMatches
            .Where(i => i.LastSixSeason?.HomeTeam?.HalfTimeWithOneGoal > percentage &&
                                i.LastSixSeason.AwayTeam?.HalfTimeWithOneGoal > percentage);
        
        nextMatchesWithGoalInFirstHalf.AddRange(matchesWithGoalInFirstHalf);
        
        var nextMatchesWithTwoGoals = new List<NextMatch2>();
        var matchesWithTwoGoals = nextMatches
            .Where(i => i.LastSixSeason?.HomeTeam?.TwoGoals > percentage &&
                        i.LastSixSeason.AwayTeam?.TwoGoals > percentage);
        
        nextMatchesWithTwoGoals.AddRange(matchesWithTwoGoals);
        
        var nextMatchesWithZeroZero = new List<NextMatch2>();
        var matchesWithZeroZero = nextMatches
            .Where(i => i.LastSixSeason?.HomeTeam?.ZeroZero < 10 &&
                        i.LastSixSeason.AwayTeam?.ZeroZero < 10);
        
        nextMatchesWithZeroZero.AddRange(matchesWithZeroZero);
        
        var nextMatchesWithTwoToThree = new List<NextMatch2>();
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

     /*   if (commonGamesWithGoalsAndLessZeroZero.Any())
        {
            Console.WriteLine("################ -- Qualified matches for all three options 'Both Team Score' or 'More Than 2 Goals' or 'At least one Goal in first half'  -- ################");
            commonGamesWithGoalsAndLessZeroZero.ForEach(i => Console.Write("{0}\t", i));
            return;

        }
*/
        var commonGamesWithZeroZero = nextMatchesWithTwoToThree
            .Where(i => nextMatchesWithZeroZero.Contains(i))
            .ToList();
        
        if (commonGamesWithZeroZero.Any())
        {
            Console.WriteLine("################ -- Qualified matches for 'Two to three goals'   -- ################");
            var topfivecommonGamesWithZeroZero = commonGamesWithZeroZero
                .OrderByDescending(i => i.LastSixSeason.HomeTeam.TwoToThree).TakeLast(5).ToList();
            var jsonString = JsonSerializer.Serialize(topfivecommonGamesWithZeroZero);
            Console.WriteLine(jsonString);
        }

        if (nextMatchesWithBothScore.Any())
        {
            if (!nextMatchesWithBothScore.Any(i => nextMatchesWithZeroZero.Contains(i)))
                return;

            var topFive = nextMatchesWithBothScore.OrderByDescending(i => i.LastSixSeason?.HomeTeam?.OneGoal2).TakeLast(5).ToList();
            Console.WriteLine("################ -- Qualified matches for 'Both Team Score'   -- ################");
            var jsonString = JsonSerializer.Serialize(topFive);
            Console.WriteLine(jsonString);
        }

        if (!nextMatchesWithTwoGoals.Any(i => commonGamesWithZeroZero.Contains(i)))
            return;
        
        var topFiveMoreThanTwoGoals = commonGamesWithGoalsAndLessZeroZero.OrderByDescending(i => i.LastSixSeason?.HomeTeam?.TwoGoals).TakeLast(5).ToList();
        Console.WriteLine("################ -- Qualified matches for 'More Than two Goals'   -- ################");
        var output3 = JsonSerializer.Serialize(topFiveMoreThanTwoGoals);
        Console.WriteLine(output3);
        
        
        

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
    /// <param name="lastSixGames"></param>
    /// <param name="currentSeasonPercentage"></param>
    /// <returns>If the zero zero result is less than 10 and at least one goal pass the passing percentage</returns>
    internal static GameAverage2? TeamPerformance(
        this IList<GameData> matches,
        string team,
        bool isHome,
        bool overAll,
        bool lastSixGames
    )
    {
        var result = new GameAverage2();
        var teamMatches = overAll ?
            matches.GetMatchesBy(a => a.HomeTeam == team || a.AwayTeam == team):
            matches.GetMatchesBy(a => isHome ? a.HomeTeam == team : a.AwayTeam == team);

        if (lastSixGames)
        {
            teamMatches = teamMatches.GetCurrentSeasonBy()
                .OrderByDescending(i => i.Date)
                .TakeLast(6)
                .ToList();
        }

        //result.OneGoal = teamMatches.AnalyseGoalsInFullTime(team, isHome, overAll, 1);
        
        // At least over 50% should make a goal in full time
        result.OneGoal2 = teamMatches.GoalsInFullTime(team, isHome, overAll, 1);
        
        // Two goals over 45% in full time
        result.TwoGoals = teamMatches.GoalsInFullTime(team, isHome, overAll, 2);
        
        // At least over 25% should make a gaol in halftime
        result.HalfTimeWithOneGoal = teamMatches.GoalsInFirstHalf(isHome, overAll);
        
        // 0:0 games are less than 10% 
        result.ZeroZero = teamMatches.ZeroZeroGoal();
        
        result.TwoToThree = teamMatches.TwoToThreeGoals();

        result.AllowGoal = teamMatches.AllowedGoal(isHome);
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
    internal static Head2HeadAverage2 AnalyseHeadToHeadPerformance(
        this IList<GameData> matches, 
        string homeTeam,
        string awayTeam,
        bool lastSixGames
    )
    {
        var atHomeMatches = matches.GetMatchesBy(a => a.HomeTeam == homeTeam && a.AwayTeam == awayTeam);

        var result = new Head2HeadAverage2
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
    
    internal static IList<GameData> GetMatchesBy(this IEnumerable<GameData> games, Func<GameData, bool> predicate)
    {
        var currentSession = games
            .Where(predicate)
            .ToList();

        return currentSession;
    }
    
    private static double ZeroZeroGoal(this IEnumerable<GameData> matches) =>
        matches.Percent(p =>  p is { FTHG: 0, FTAG: 0 });

    private static double BothTeamMakeGoal(this IEnumerable<GameData> matches) =>
        matches.Percent(a => a is { FTHG: > 0, FTAG: > 0 });
    
    private static double GoalsInFirstHalf(this IEnumerable<GameData> matches, bool isHome, bool overAll)
    {
        return overAll 
            ? matches.Percent(a => a.HTHG > 0 || a.HTAG > 0)
            : matches.Percent(a => isHome ? a.HTHG > 0 : a.HTAG > 0);
    }
    
    private static double GoalsInFullTime(
        this IEnumerable<GameData> matches,
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
    
   
    private static double MoreThanTwoGoals(this IEnumerable<GameData> matches) =>
        matches.Percent(a =>  a.FTHG + a.FTAG >= 3);
    
    private static double TwoToThreeGoals(this IEnumerable<GameData> matches) =>
        matches.Percent(a => a.FTHG + a.FTAG <= 3 && a.FTHG + a.FTAG > 1);
    
    private static double AllowedGoal(this IEnumerable<GameData> matches, bool isHome) =>
        matches.Percent(a => isHome ? a.FTAG != 0 : a.FTHG != 0);
   
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
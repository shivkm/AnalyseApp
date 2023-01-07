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

    internal static void FindTopMatchesForBothTeamScore(this IEnumerable<NextMatch> nextMatches, double percentage)
    {
        var overallBothTeamGoal = nextMatches
            .Where(i => i.HomeOverAllAverage?.AtLeastOneGoal > percentage &&
                        i.AwayOverAllAverage?.AtLeastOneGoal > percentage &&
                        i.HomeOverAllAverage?.ZeroZero < 10 && i.AwayOverAllAverage?.ZeroZero < 10).ToList();


        overallBothTeamGoal.ForEach(i =>
            i.Msg = i.HomeOverAllAverage?.ZeroZero == 0 || i.AwayOverAllAverage?.ZeroZero == 0
                ? "statistic cannot analyse 0:0 game. data is missing"
                : ""
        );

        if (!overallBothTeamGoal.Any()) return;

        var currentSeasonBothTeamGoal = overallBothTeamGoal
            .Where(i => i.HomeCurrentAverage?.AtLeastOneGoal > percentage &&
                        i.AwayCurrentAverage?.AtLeastOneGoal > percentage)
            .ToList();

        if (!currentSeasonBothTeamGoal.Any()) return;

        var headToHeadOverall = currentSeasonBothTeamGoal
            .Where(i => i.HeadToHeadAverage?.BothTeamScore > percentage)
            .ToList();

        if (!headToHeadOverall.Any()) return;

        var headToHeadCurrentSeason = headToHeadOverall
            .Where(i => i.HeadToHeadAverage?.BothTeamScore > percentage)
            .ToList();

        if (!headToHeadCurrentSeason.Any()) return;
        
        Console.WriteLine("################ -- matches qualified for both team make score  -- ################ \n");
        headToHeadCurrentSeason.ForEach(i => Console.Write("{0}\t\n", i));

    }

    internal static void FindTopMatchesForMoreThanTwoGoal(this IEnumerable<NextMatch> nextMatches, double percentage)
    {
        var overallBothTeamGoal = nextMatches
            .Where(i => i.HomeOverAllAverage?.MoreThanTwoGoals > percentage &&
                        i.AwayOverAllAverage?.MoreThanTwoGoals > percentage &&
                        i.HomeOverAllAverage?.ZeroZero < 10 && i.AwayOverAllAverage?.ZeroZero < 10).ToList();


        overallBothTeamGoal.ForEach(i =>
            i.Msg = i.HomeOverAllAverage?.ZeroZero == 0 || i.AwayOverAllAverage?.ZeroZero == 0
                ? "statistic cannot analyse 0:0 game. data is missing"
                : ""
        );

        if (!overallBothTeamGoal.Any()) return;

        var currentSeasonBothTeamGoal = overallBothTeamGoal
            .Where(i => i.HomeCurrentAverage?.MoreThanTwoGoals > percentage &&
                        i.AwayCurrentAverage?.MoreThanTwoGoals > percentage)
            .ToList();

        if (!currentSeasonBothTeamGoal.Any()) return;

        var headToHeadOverall = currentSeasonBothTeamGoal
            .Where(i => i.HeadToHeadAverage?.MoreThanTwoGoals > percentage)
            .ToList();

        if (!headToHeadOverall.Any()) return;

        var headToHeadCurrentSeason = headToHeadOverall
            .Where(i => i.HeadToHeadAverage?.MoreThanTwoGoals > percentage)
            .ToList();

        if (!headToHeadCurrentSeason.Any()) return;
        
        Console.WriteLine("################ -- matches qualified for more than two goals  -- ################ \n");
        headToHeadCurrentSeason.ForEach(i => Console.Write("{0}\t\n", i));

    }
    
    
    internal static void FindTopMatchesForTwoToThreeGoal(this IEnumerable<NextMatch> nextMatches, double percentage)
    {
        var overallBothTeamGoal = nextMatches
            .Where(i => i.HomeOverAllAverage?.TwoToThree > percentage &&
                        i.AwayOverAllAverage?.TwoToThree > percentage &&
                        i.HomeOverAllAverage?.ZeroZero < 10 && i.AwayOverAllAverage?.ZeroZero < 10).ToList();


        overallBothTeamGoal.ForEach(i =>
            i.Msg = i.HomeOverAllAverage?.ZeroZero == 0 || i.AwayOverAllAverage?.ZeroZero == 0
                ? "statistic cannot analyse 0:0 game. data is missing"
                : ""
        );

        if (!overallBothTeamGoal.Any()) return;

        var currentSeasonBothTeamGoal = overallBothTeamGoal
            .Where(i => i.HomeCurrentAverage?.TwoToThree > percentage &&
                        i.AwayCurrentAverage?.TwoToThree > percentage)
            .ToList();

        if (!currentSeasonBothTeamGoal.Any()) return;

        var headToHeadOverall = currentSeasonBothTeamGoal
            .Where(i => i.HeadToHeadAverage?.TwoToThree > percentage)
            .ToList();

        if (!headToHeadOverall.Any()) return;

        var headToHeadCurrentSeason = headToHeadOverall
            .Where(i => i.HeadToHeadAverage?.TwoToThree > percentage)
            .ToList();

        if (!headToHeadCurrentSeason.Any()) return;
        
        Console.WriteLine("################ -- matches qualified for two to three goals  -- ################ \n");
        headToHeadCurrentSeason.ForEach(i => Console.Write("{0}\t\n", i));

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
    /// <param name="passingPercentage">passing percentage to avoid bad performing games</param>
    /// <returns>If the zero zero result is less than 10 and at least one goal pass the passing percentage</returns>
    internal static GameAverage TeamPerformance(
        this IList<Matches> matches,
        string team,
        bool isHome,
        int passingPercentage
    )
    {
        var result = new GameAverage();
        var teamMatches = matches.GetMatchesBy(a => isHome ? a.HomeTeam == team : a.AwayTeam == team);
        
        result.AtLeastOneGoal = teamMatches.GetPercentageOfTeamWithExpectedGoal(isHome, 1);
        result.ZeroZero = teamMatches.ZeroZeroGoal();
        
        // Zero zero result should be less then 10% and
        // analysis overall + field side should be more than passing percentage
        if (result.ZeroZero > 10 || result.AtLeastOneGoal < passingPercentage)
            return new GameAverage();
        
        result.MoreThanTwoGoals = teamMatches.GetPercentageOfTeamWithExpectedGoal(isHome, 2);
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
        var atHomeMatches = matches
            .GetMatchesBy(a => a.HomeTeam == homeTeam && a.AwayTeam == awayTeam);

        var zeroZero = atHomeMatches.ZeroZeroGoal();
        var bothTeamScoreAtHome = atHomeMatches.BothTeamMakeGoal();
        var moreThanTwoGoalsAtHome = atHomeMatches.MoreThanTwoGoals();
        var twoToThreeAtHome = atHomeMatches.TwoToThreeGoals();

        var hint = "";
        if (zeroZero < 10)
        {
            if (bothTeamScoreAtHome > 50)
                hint = "over 50% both team score";

            if (moreThanTwoGoalsAtHome > 40)
                hint = $"{hint} and over 40% more than two goals";

            if (twoToThreeAtHome > 75)
                hint = $"{hint}\n over 75% two to three goals";
        }
        
        var result = new Head2HeadAverage
        {
            ZeroZero = zeroZero,
            BothTeamScore = bothTeamScoreAtHome,
            MoreThanTwoGoals = moreThanTwoGoalsAtHome,
            TwoToThree = twoToThreeAtHome,
            Hint = hint
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
    
    
    private static double GoalsInFirstHalf(this IEnumerable<Matches> matches) =>
        matches.Percent(a => a is { HTHG: > 0, HTAG: > 0 });
    
    private static double GoalsInFirstHalf(this IEnumerable<Matches> matches, bool isHome) =>
        matches.Percent(a => isHome ? a.HTHG > 0 : a.HTAG > 0);
    
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
        var result = matches
            .Percent(p => isHome ? p.FTHG >= expectedGoal : p.FTAG >= expectedGoal);

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
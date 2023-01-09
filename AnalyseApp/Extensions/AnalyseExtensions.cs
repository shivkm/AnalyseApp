using AnalyseApp.models;

namespace AnalyseApp.Extensions;

public static class AnalyseExtensions
{
    /// <summary>
    /// This will filter the matches where home and away team played and analyse following points
    ///     * at least one goal overall and at home or away depend on where the team is playing
    ///     * two or more goals overall and at home or away depend on where the team is playing
    ///     * Two to three goals overall and at home or away depend on where the team is playing
    ///     * zero zero games overall and at home or away depend on where the team is playing
    /// </summary>
    /// <param name="gameData">Historical or current season matches</param>
    /// <param name="homeTeam">Home team</param>
    /// <param name="awayTeam">Away team</param>
    /// <returns></returns>
    internal static Head2HeadAverage AnalyseHeadToHead(this IList<GameData> gameData, string homeTeam, string awayTeam)
    {
        gameData = gameData.GetMatchesBy(a => a.HomeTeam == homeTeam && a.AwayTeam == awayTeam);

        var count = gameData.Count;
        var result = new Head2HeadAverage
        {
            ZeroZero = gameData.ZeroZeroGoal(),
            BothTeamScore = gameData.BothTeamMakeGoal(),
            MoreThanTwoGoals = gameData.MoreThanTwoGoals(),
            TwoToThree = gameData.TwoToThreeGoals(),
            GoalInFirstHalf = gameData.GoalsInFirstHalf(false, true),
        };

        if (result.ZeroZero > 30)
            result.Msg = gameData.Count > 5 ? "" : $"Not enough data for analysis! result based on {count}";

        if (result.BothTeamScore < 60)
            result.Msg = $"{result.Msg}\ndoesn't qualified for both team score " +
                         $"{result.BothTeamScore}% out of 60%. Analysed the {count} games";
        
        if (result.MoreThanTwoGoals < 50)
            result.Msg = $"{result.Msg}\ndoesn't qualified for more than two score" +
                         $" {result.MoreThanTwoGoals}% out of 50%. Analysed the {count} games";
        
        if (result.TwoToThree < 50)
            result.Msg = $"{result.Msg}\ndoesn't qualified for two to three score" +
                         $" {result.TwoToThree}% out of 50%. Analysed the {count} games";
        
        if (result.GoalInFirstHalf < 45)
            result.Msg = $"{result.Msg}\ndoesn't score in first halftime" +
                         $" {result.GoalInFirstHalf}% out of 45%. Analysed the {count} games";
        
        return result;
    }
    
    
    /// <summary>
    /// This method will analyse the team in over all and at it's field
    ///     First calculate average of given expected goal in overall games such as one
    ///     Than calculate average of given expected goal in its field such as home or away
    ///     Secondly calculate average of given expected goal in halftime in its field
    ///     Than calculate average of given expected goal in halftime in overall games
    ///
    ///     This calculation will be weighted in ratio of 40/60 the field has the weight of 60%
    ///     After those calculation it should qualified base on passing percentage given in NextGame object
    ///     is that qualified or not if not what is the cause for that and if it does that what is the cause for that
    ///     as well.
    ///
    ///     Based on the expected goal it will use the expected passing percentage and generate the message
    ///     For one goal and two goal will be use the given expected passing percentage
    ///     For the halftime it will reduce 10% of expected passing percentage of one goal
    ///     Specific  Field also reduce 5% of expected passing percentage
    /// </summary>
    /// <param name="gameData"></param>
    /// <param name="nextGame"></param>
    /// <param name="expectedGoal"></param>
    /// <returns>The ratio calculated value</returns>
    private static (decimal fulltime, decimal halftime) TeamWithExpectedGoal(this IList<GameData> gameData, NextGame nextGame, int expectedGoal)
    {
        var homeTeamOneGoal = gameData
            .AnalyseGoalsInFullTime(nextGame.Team, nextGame.IsHome, true, expectedGoal, default);
        
        var homeTeamOneWithGoalInField = gameData
            .AnalyseGoalsInFullTime(nextGame.Team, nextGame.IsHome, default, expectedGoal, default);

        var halfTimeGoal = gameData
            .AnalyseGoalsInFullTime(nextGame.Team, nextGame.IsHome, true, 1, true);
        
        var halfTimeGoalInField = gameData
            .AnalyseGoalsInFullTime(nextGame.Team, nextGame.IsHome, default, 1, true);

        var halftimeScore = halfTimeGoal.percentage * 0.40m + halfTimeGoalInField.percentage * 0.60m;
        
        var field = nextGame.IsHome ? "Home" : "Away";
        var result = 0.0m;
        switch (expectedGoal)
        {
            case 1:
            {

                if (halftimeScore < nextGame.ExpectedPercentageForOneGoal - 10)
                    nextGame.Msg = $"{nextGame.Team} scored below {halftimeScore}% a goal in first halftime in last " +
                                   $"{nextGame.TakeLastGames} games which is less than" +
                                   $" {nextGame.ExpectedPercentageForOneGoal}%";
                
                if (homeTeamOneGoal.percentage < nextGame.ExpectedPercentageForOneGoal)
                    nextGame.Msg = $"{nextGame.Team} scored below {homeTeamOneGoal.percentage}% a goal in last " +
                                   $"{nextGame.TakeLastGames} games which is less than" +
                                   $" {nextGame.ExpectedPercentageForOneGoal}%";
        
                if (homeTeamOneWithGoalInField.percentage < nextGame.ExpectedPercentageForOneGoal - 5)
                    nextGame.Msg = $"{nextGame.Msg}\n{nextGame.Team} scored below {homeTeamOneGoal.percentage}% a goal " +
                                   $"in last {homeTeamOneWithGoalInField.totalGames} at {field} field games" +
                                   $" which is less than {nextGame.ExpectedPercentageForOneGoal-5}%";
                
                var oneGoal = homeTeamOneGoal.percentage * 0.40m + homeTeamOneWithGoalInField.percentage * 0.60m;
                result = oneGoal * 0.50m + halftimeScore * 0.50m;
                break;
            }
            case 2:
            {
                if (homeTeamOneGoal.percentage < nextGame.ExpectedPercentageForTwoGoal)
                    nextGame.Msg = $"{nextGame.Msg}\n{nextGame.Team} scored below {homeTeamOneGoal}% two goals " +
                                   $"in last {nextGame.TakeLastGames} games which is" +
                                   $" less than {nextGame.ExpectedPercentageForTwoGoal}%";
        
                if (homeTeamOneWithGoalInField.percentage < nextGame.ExpectedPercentageForTwoGoal - 5)
                    nextGame.Msg = $"{nextGame.Msg}\n{nextGame.Team} scored below {homeTeamOneGoal}% two goals " +
                                   $"in last {homeTeamOneWithGoalInField.totalGames} at {field} field games" +
                                   $" which is less than {nextGame.ExpectedPercentageForTwoGoal-5}%";
        
                var twoGoal = homeTeamOneGoal.percentage * 0.40m + homeTeamOneWithGoalInField.percentage * 0.60m;
                result = twoGoal * 0.45m + halftimeScore * 0.55m;
                break;
            }
        }

        return (Math.Round(result, 2), Math.Round(halftimeScore, 2));
    }
    
    public static Average AnalyseGamesBy(this IList<GameData> gameData, NextGame nextGame)
    {
        
        var oneGoal = gameData.TeamWithExpectedGoal(nextGame, 1);
        var twoGoals = gameData.TeamWithExpectedGoal(nextGame, 2);

        gameData.AnalysisAllowedGoal(nextGame);

        if (oneGoal.fulltime < nextGame.ExpectedPercentageForOneGoal)
            nextGame.Msg = $"{nextGame.Msg}\n{nextGame.Team} scored below {oneGoal.fulltime}% a goal in last {nextGame.TakeLastGames} games.";

        if (twoGoals.fulltime < nextGame.ExpectedPercentageForTwoGoal)
        {
            nextGame.Msg = $"{nextGame.Msg}\n{nextGame.Team} scored below {twoGoals.fulltime}% two goals in last {nextGame.TakeLastGames} games";
        }
        
        var qualified = false;
        if (oneGoal.fulltime > nextGame.ExpectedPercentageForOneGoal && string.IsNullOrWhiteSpace(nextGame.Msg))
        {
            nextGame.Msg = $"{nextGame.Team} scored over {oneGoal.fulltime}% a goal in last {nextGame.TakeLastGames} games";
            qualified = true;
        }
        if (twoGoals.fulltime > nextGame.ExpectedPercentageForTwoGoal)
        {
            nextGame.Msg = $"{nextGame.Msg}\n{nextGame.Team} scored over {twoGoals.fulltime}% two goals in last {nextGame.TakeLastGames} games";
            qualified = true;
        }
        
        var result = new Average(oneGoal.fulltime, twoGoals.fulltime,  oneGoal.halftime, qualified, nextGame.Msg);
        return result;
    }
    
    internal static IList<GameData> GetGamesDataBy(this IList<GameData> gameData, NextGame nextGame)
    {
        var games = gameData
            .GetMatchesBy(a => a.HomeTeam == nextGame.Team || a.AwayTeam == nextGame.Team);

        if (nextGame.TakeLastGames != 0)
            games = games.Take(nextGame.TakeLastGames).ToList();
        
        return games;
    }

    internal static (decimal? Average, bool Qaulified) PredictBothTeamScore(
        this GameAverage allGames,
        GameAverage lastSixGames,
        GameAverage lastTwelveGames,
        Head2HeadAverage headToHead
    )
    {
        var homeOneGoal = allGames.Home?.OneGoalPercentage * 0.33m +
                                lastSixGames.Home?.OneGoalPercentage * 0.33m +
                                lastTwelveGames.Home?.OneGoalPercentage * 0.33m;
        
        var awayOneGoal = allGames.Away?.OneGoalPercentage * 0.33m +
                          lastSixGames.Away?.OneGoalPercentage * 0.33m +
                          lastTwelveGames.Away?.OneGoalPercentage * 0.33m;

        var result = homeOneGoal * 0.30m + awayOneGoal * 0.30m
                                         + headToHead.BothTeamScore * 0.40m;

        var homeQualified = allGames.Home!.Qualified && lastSixGames.Home!.Qualified && lastTwelveGames.Home!.Qualified;
        var awayQualified = allGames.Away!.Qualified && lastSixGames.Away!.Qualified && lastTwelveGames.Away!.Qualified;
        
        return (result, homeQualified && awayQualified);
    }
    
    internal static (decimal? Average, bool Qaulified) PredictMoreThanTwoScore(
        this GameAverage allGames,
        GameAverage lastSixGames,
        GameAverage lastTwelveGames,
        Head2HeadAverage headToHead
    )
    {
        var homeOneGoal = allGames.Home?.TwoGoalPercentage * 0.33m +
                          lastSixGames.Home?.TwoGoalPercentage * 0.33m +
                          lastTwelveGames.Home?.TwoGoalPercentage * 0.33m;
        
        var awayOneGoal = allGames.Away?.TwoGoalPercentage * 0.33m +
                          lastSixGames.Away?.TwoGoalPercentage * 0.33m +
                          lastTwelveGames.Away?.TwoGoalPercentage * 0.33m;

        var result = homeOneGoal * 0.30m + awayOneGoal * 0.30m
                                         + headToHead.MoreThanTwoGoals * 0.40m;

        var homeQualified = allGames.Home!.Qualified && lastSixGames.Home!.Qualified && lastTwelveGames.Home!.Qualified;
        var awayQualified = allGames.Away!.Qualified && lastSixGames.Away!.Qualified && lastTwelveGames.Away!.Qualified;
        
        return (result, homeQualified && awayQualified);
    }
    
    internal static string? PredictTwoToThreeScore(
        this GameAverage allGames,
        GameAverage lastSixGames,
        GameAverage lastTwelveGames,
        Head2HeadAverage headToHead
    )
    {
        var oneGoal = allGames
            .PredictBothTeamScore(lastTwelveGames, lastSixGames, headToHead);
        
        var twoGoal = allGames
            .PredictMoreThanTwoScore(lastTwelveGames, lastSixGames, headToHead);

        var result = oneGoal.Average is < 60 and > 50 && twoGoal.Average is > 40 and < 50 && headToHead.TwoToThree > 55;

        var msg = string.Empty;
        if (result)
            msg =  $"Analysis said {oneGoal.Average}% that both team make score a goal each or {twoGoal.Average}% for two or more goals." +
                   "through the validation this game qualified for two to three goals";

        return msg;
    }
    
    /// <summary>
    ///  This method will first filter the games where team doesn't allow his opponents to score a goal
    ///  Than count the number of games to create a msg which can be used for making any decision
    /// </summary>
    /// <param name="games">List of the Games</param>
    /// <param name="nextGame">Current Team and analyse data</param>
    private static void AnalysisAllowedGoal(this IList<GameData> games, NextGame nextGame)
    {
        var notAllowedGoalPercentage = games
            .GetPercent(a => a.HomeTeam == nextGame.Team && a.FTAG == 0 ||
                             a.AwayTeam == nextGame.Team && a.FTHG == 0);

        var notAllowedGoal = games
            .Count(a => a.HomeTeam == nextGame.Team && a.FTAG == 0 ||
                        a.AwayTeam == nextGame.Team && a.FTHG == 0);

        if (notAllowedGoalPercentage > nextGame.ExpectedPercentageForOneGoal)
            nextGame.Msg = $"{nextGame.Msg}\n{nextGame.Team} doesn't allowed any goal in last {notAllowedGoal} games";
    }

    private static (decimal percentage, int totalGames) AnalyseGoalsInFullTime(
        this IList<GameData> matches,
        string team,
        bool isHome, 
        bool overall, 
        int expectedGoal,
        bool halfTimeGoal
    )
    {
        var totalGames = matches.Count(i => isHome ? i.HomeTeam == team : i.AwayTeam == team);
        var percentage = matches.Where(i => i.HomeTeam == team).GetPercent(p => halfTimeGoal ? p.HTHG >= expectedGoal : p.FTHG >= expectedGoal);
        
        if (overall)
        {
            totalGames = matches.Count(i => i.HomeTeam == team || i.AwayTeam == team);
            percentage = matches.GetPercent(a => halfTimeGoal 
                ? a.HomeTeam == team && a.HTHG >= expectedGoal || a.AwayTeam == team && a.HTAG >= expectedGoal 
                : a.HomeTeam == team && a.FTHG >= expectedGoal || a.AwayTeam == team && a.FTAG >= expectedGoal);
        }
        
        var result = (percentage, totalGames);

        return result;
    }
    
    private static decimal ZeroZeroGoal(this IEnumerable<GameData> matches) =>
        matches.GetPercent(p =>  p is { FTHG: 0, FTAG: 0 });

    private static decimal BothTeamMakeGoal(this IEnumerable<GameData> matches) =>
        matches.GetPercent(a => a is { FTHG: > 0, FTAG: > 0 });
    
    private static decimal MoreThanTwoGoals(this IEnumerable<GameData> matches) =>
        matches.GetPercent(a =>  a.FTHG + a.FTAG >= 3);

    private static decimal TwoToThreeGoals(this IEnumerable<GameData> matches) =>
        matches.GetPercent(a => a.FTHG + a.FTAG <= 3 && a.FTHG + a.FTAG > 1);

    private static decimal GoalsInFirstHalf(this IEnumerable<GameData> matches, bool isHome, bool overAll)
    {
        return overAll 
            ? matches.GetPercent(a => a.HTHG > 0 || a.HTAG > 0)
            : matches.GetPercent(a => isHome ? a.HTHG > 0 : a.HTAG > 0);
    }


    private static decimal GetPercent<T>(this IEnumerable<T> source, Func<T, bool> predicate)
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

        return 100.00m * total / count;
    }
}
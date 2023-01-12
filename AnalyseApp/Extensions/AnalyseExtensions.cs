using System.Runtime.InteropServices.JavaScript;
using System.Text;
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
        var zeroZeroGames = gameData.ZeroZeroGoal().GetValue();
        var bothTeamScore = gameData.BothTeamMakeGoal().GetValue();
        var moreThanTwoGoals = gameData.MoreThanTwoGoals().GetValue();
        var twoToThree = gameData.TwoToThreeGoals().GetValue();
        var goalInFirstHalfHome = gameData
            .AnalyseGoals(homeTeam, true, default, 1, true);
        var goalInFirstHalfAway = gameData
            .AnalyseGoals(homeTeam, default, default, 1, true);
        
        var result = new Head2HeadAverage
        {
            ZeroZero = $"{zeroZeroGames}% could be 0:0 out of {count} head to head games",
            BothTeamScore = $"{bothTeamScore}% scored both team a goal in last {count} head to head games",
            MoreThanTwoGoals = $"{moreThanTwoGoals}% it {count} head to head games",
            TwoToThree = $"{twoToThree}% scored both team two to three goals in last {count} head to head games",
            GoalInFirstHalf = $"Home scored {goalInFirstHalfHome}% in halftime in last {count} head to head games\n" +
                              $"Away scored {goalInFirstHalfAway}% in halftime in last {count} head to head games",
            
            ZeroZeroQualified = zeroZeroGames > 35,
            BothTeamScoreQualified = zeroZeroGames < 30 && bothTeamScore >= 60,
            MoreThanTwoGoalsQualified = zeroZeroGames < 30 && moreThanTwoGoals >= 50,
            TwoToThreeQualified = zeroZeroGames < 30 && twoToThree >= 50,
            GoalInFirstHalfQualified = zeroZeroGames < 30 && goalInFirstHalfHome.percentage + goalInFirstHalfAway.percentage >= 50
        };

        return result;
    }

    private static decimal GetValue(this decimal value) => Math.Round(value, 2);
    
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
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, true, expectedGoal, default);
        
        var homeTeamOneWithGoalInField = gameData
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, default, expectedGoal, default);

        var halfTimeGoal = gameData
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, true, 1, true);
        
        var halfTimeGoalInField = gameData
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, default, 1, true);

        var halftimeScore = halfTimeGoal.percentage * 0.40m + halfTimeGoalInField.percentage * 0.60m;
        
        var field = nextGame.IsHome ? "Home" : "Away";
        var tex = $"Scored one goal {homeTeamOneGoal.percentage}% in last {homeTeamOneGoal.totalGames}";
        var tex2 = $"Scored two goal {homeTeamOneGoal.percentage}% in last {homeTeamOneGoal.totalGames}";
        var result = 0.0m;
        switch (expectedGoal)
        {
            case 1:
            {
                
                if (halftimeScore < nextGame.ExpectedPercentageForOneGoal - 10)
                    nextGame.Msg = $"{nextGame.Team} scored {halftimeScore.GetValue()}% a goal in first halftime in last " +
                                   $"{nextGame.TakeLastGames} games";
                
                if (homeTeamOneGoal.percentage < nextGame.ExpectedPercentageForOneGoal)
                    nextGame.Msg = $"{nextGame.Team} scored {homeTeamOneGoal.percentage}% a goal in last " +
                                   $"{nextGame.TakeLastGames} games which is less than" +
                                   $" {nextGame.ExpectedPercentageForOneGoal}%";
        
                if (homeTeamOneWithGoalInField.percentage < nextGame.ExpectedPercentageForOneGoal - 5)
                    nextGame.Msg = $"{nextGame.Msg} {nextGame.Team} scored below {homeTeamOneGoal.percentage}% a goal " +
                                   $"in last {homeTeamOneWithGoalInField.totalGames} at {field} field games" +
                                   $" which is less than {nextGame.ExpectedPercentageForOneGoal-5}%";
                
                var oneGoal = homeTeamOneGoal.percentage * 0.40m + homeTeamOneWithGoalInField.percentage * 0.60m;
                result = oneGoal * 0.50m + halftimeScore * 0.50m;
                break;
            }
            case 2:
            {
                if (homeTeamOneGoal.percentage < nextGame.ExpectedPercentageForTwoGoal)
                    nextGame.Msg = $"{nextGame.Msg}\n{nextGame.Team} scored below {homeTeamOneGoal.percentage}% two goals " +
                                   $"in last {nextGame.TakeLastGames} games which is" +
                                   $" less than {nextGame.ExpectedPercentageForTwoGoal}%";
        
                if (homeTeamOneWithGoalInField.percentage < nextGame.ExpectedPercentageForTwoGoal - 5)
                    nextGame.Msg = $"{nextGame.Msg}\n{nextGame.Team} scored below {homeTeamOneGoal.percentage}% two goals " +
                                   $"in last {homeTeamOneWithGoalInField.totalGames} at {field} field games" +
                                   $" which is less than {nextGame.ExpectedPercentageForTwoGoal-5}%";
        
                var twoGoal = homeTeamOneGoal.percentage * 0.40m + homeTeamOneWithGoalInField.percentage * 0.60m;
                result = twoGoal * 0.45m + halftimeScore * 0.55m;
                break;
            }
        }

        return (result.GetValue(), halftimeScore.GetValue());
    }
    
    public static Average AnalyseTeamGoals(this IList<GameData> gameData, NextGame nextGame)
    {
        var halfTimeGoal = gameData
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, true, 1, true);
        
        var halfTimeGoalInField = gameData
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, default, 1, true);

        var teamOneGoal = gameData
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, true, 1, default);
        
        var teamOneWithGoalInField = gameData
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, default, 1, default);
        
        var teamTwoGoal = gameData
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, true, 2, default);
        
        var teamTwoWithGoalInField = gameData
            .AnalyseGoals(nextGame.Team, nextGame.IsHome, default, 2, default);

        var wonGames = gameData.GetPercent(i => i.HomeTeam == nextGame.Team && i.FTR == "H" || 
                                                i.AwayTeam == nextGame.Team && i.FTR == "A");
        var twoToThreeGames = gameData.TwoToThreeGoals().GetValue();
        var halftimeScore = halfTimeGoal.percentage.GetWeightedValue(halfTimeGoalInField.percentage);
        var expectedOneScore = teamOneGoal.percentage.GetWeightedValue(teamOneWithGoalInField.percentage);
        var expectedTwoScore = teamTwoGoal.percentage.GetWeightedValue(teamTwoWithGoalInField.percentage);
        
        var field = nextGame.IsHome ? "Home" : "Away";
        
        // Create msg to see why or why not qualified
        var sb = new StringBuilder();
        sb.Append($"{teamOneGoal.percentage}% overall and {teamOneWithGoalInField.percentage}% scored a goal at" +
                  $" {field} in last {teamOneGoal.totalGames} overall and {teamOneWithGoalInField.totalGames} games");
        
        sb.Append($"{halfTimeGoal.percentage}% overall and {halfTimeGoalInField.percentage}% scored a goal " +
                  $"at {field} in last {halfTimeGoal.totalGames} overall and {halfTimeGoalInField.totalGames} games");
        
        var notAllowedGoalPercentage = gameData
            .GetPercent(a => a.HomeTeam == nextGame.Team && a.FTAG == 0 ||
                             a.AwayTeam == nextGame.Team && a.FTHG == 0);

        var zeroZeroGames = gameData.ZeroZeroGoal().GetValue();
          
        sb.Clear();
        return new Average(
            expectedOneScore,
            expectedTwoScore,
            halftimeScore,
            twoToThreeGames,
            wonGames,
            zeroZeroGames,
            notAllowedGoalPercentage,
            sb.ToString());
    }

    private static decimal GetWeightedValue(this decimal left, decimal right) => left * 0.40m + right * 0.60m;

    
    internal static IList<GameData> GetGamesDataBy(this IList<GameData> gameData, NextGame nextGame)
    {
        var games = gameData
            .GetMatchesBy(a => a.HomeTeam == nextGame.Team || a.AwayTeam == nextGame.Team);

        if (nextGame.TakeLastGames != 0)
            games = games.Take(nextGame.TakeLastGames).ToList();
        
        return games;
    }

   
    
    internal static string GetValue<T>(this T value) 
        => value.ToString().Replace("_", " ");
    
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

    private static (decimal percentage, int totalGames) AnalyseGoals(
        this IList<GameData> matches,
        string team,
        bool isHome, 
        bool overall, 
        int expectedGoal,
        bool halfTimeGoal
    )
    {
        var totalGames = matches.Count(i => isHome ? i.HomeTeam == team : i.AwayTeam == team);
        var percentage = matches.Where(i => i.HomeTeam == team)
            .GetPercent(p => halfTimeGoal ? p.HTHG >= expectedGoal : p.FTHG >= expectedGoal);
        
        if (overall)
        {
            totalGames = matches.Count(i => i.HomeTeam == team || i.AwayTeam == team);
            percentage = matches.GetPercent(a => halfTimeGoal 
                ? a.HomeTeam == team && a.HTHG >= expectedGoal || a.AwayTeam == team && a.HTAG >= expectedGoal 
                : a.HomeTeam == team && a.FTHG >= expectedGoal || a.AwayTeam == team && a.FTAG >= expectedGoal);
        }
        
        var result = (percentage.GetValue(), totalGames);

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
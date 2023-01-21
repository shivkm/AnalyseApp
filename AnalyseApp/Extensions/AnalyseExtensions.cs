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
        var zeroZeroGames = gameData.ZeroZeroGoal().GetTwoDigitAfterComma();
        var bothTeamScore = gameData.BothTeamMakeGoal().GetTwoDigitAfterComma();
        var moreThanTwoGoals = gameData.MoreThanTwoGoals().GetTwoDigitAfterComma();
        var twoToThree = gameData.TwoToThreeGoals().GetTwoDigitAfterComma();
        var goalInFirstHalfHome = gameData
            .AnalyseGoals(homeTeam, true, default, 1, true);
        var goalInFirstHalfAway = gameData
            .AnalyseGoals(homeTeam, default, default, 1, true);
        
        var homeWonGames = HomeWonGames(gameData, homeTeam);

        var awayWonGames = AwayWonGames(gameData, awayTeam);
        var halftimeMoreGoal = gameData.GetPercent(i => i.HTHG > i.FTHG || i.HTAG > i.FTAG);
        var fullTimeMoreGoal = gameData.GetPercent(i => i.FTHG > i.HTHG || i.FTAG > i.HTAG);
        
        var result = new Head2HeadAverage
        {
            ZeroZero = $"{zeroZeroGames}% could be 0:0 out of {count} head to head games",
            BothTeamScore = $"{bothTeamScore}% scored both team a goal in last {count} head to head games",
            MoreThanTwoGoals = $"{moreThanTwoGoals}% it {count} head to head games",
            TwoToThree = $"{twoToThree}% scored both team two to three goals in last {count} head to head games",
            GoalInFirstHalf = $"Home scored {goalInFirstHalfHome}% in halftime in last {count} head to head games\n" +
                              $"Away scored {goalInFirstHalfAway}% in halftime in last {count} head to head games",
            
            HomeWin = homeWonGames,
            AwayWin = awayWonGames,
            ZeroZeroQualified = zeroZeroGames > 35,
            BothTeamScoreQualified = count > 4 && zeroZeroGames < 30 && bothTeamScore >= 60,
            MoreThanTwoGoalsQualified = count > 4 && zeroZeroGames < 30 && moreThanTwoGoals >= 50,
            TwoToThreeQualified = count > 4 && zeroZeroGames < 30 && twoToThree >= 50,
         //   GoalInFirstHalfQualified = count > 4 && zeroZeroGames < 30 && goalInFirstHalfHome.percentage + goalInFirstHalfAway.percentage >= 50,
            HalftimeMoreGoal = halftimeMoreGoal,
            FullTioMoreGoal = fullTimeMoreGoal
        };

        return result;
    }

    private static decimal AwayWonGames(IList<GameData> gameData, string awayTeam)
    {
        var awayWonFullTimeGames = gameData.GetPercent(i => i.AwayTeam == awayTeam && i.FTR == "A");

        var awayWonHalfTimeGames = gameData.GetPercent(i => i.AwayTeam == awayTeam && i.HTR == "A");

        var awayWonGames = awayWonFullTimeGames.GetWeightedValue(awayWonHalfTimeGames);
        return awayWonGames;
    }

    private static decimal HomeWonGames(IList<GameData> gameData, string homeTeam)
    {
        var homeWonFullTimeGames = gameData.GetPercent(i => i.HomeTeam == homeTeam && i.FTR == "H");

        var homeWonHalfTimeGames = gameData.GetPercent(i => i.HomeTeam == homeTeam && i.HTR == "H");

        var homeWonGames = homeWonFullTimeGames.GetWeightedValue(homeWonHalfTimeGames);
        return homeWonGames;
    }

    public static decimal GetTwoDigitAfterComma(this decimal value) => Math.Round(value, 2);
    
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
    /// <returns>The ratio calculated value</returns>
    public static PoissonAverage AnalyseTeamGoals(this IList<GameData> gameData, NextGame nextGame)
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

        var wonGames = WonGames(gameData, nextGame);

        var twoToThreeGames = gameData.TwoToThreeGoals().GetTwoDigitAfterComma();
        
      /*  var halftimeScore = halfTimeGoal.percentage.GetWeightedValue(halfTimeGoalInField.percentage);
        var expectedOneScore = teamOneGoal.percentage.GetWeightedValue(teamOneWithGoalInField.percentage);
        var expectedTwoScore = teamTwoGoal.percentage.GetWeightedValue(teamTwoWithGoalInField.percentage);
*/
        var moreThanTwoGames = GamesWithMoreThanTwoGoals(gameData, nextGame);
        var TwoToThreeGoals = GamesWithTwoToThreeGoals(gameData, nextGame);
        
        var allowedGoals = gameData.GetPercent(a => a.HomeTeam == nextGame.Team && a.FTAG > 0 ||
                                                                    a.AwayTeam == nextGame.Team && a.FTHG > 0);

        var (halftimeGoals, fullTimeGoals) = AnalyseMoreGoals(gameData, nextGame.Team, nextGame.IsHome, false);
        var (halftimeGoalsOverall, fullTimeGoalsOverall) = AnalyseMoreGoals(gameData, nextGame.Team, nextGame.IsHome, true);
        var halftimeGoalsPercentage = halftimeGoals.GetWeightedValue(halftimeGoalsOverall);
        var fullTimeGoalsPercentage = fullTimeGoals.GetWeightedValue(fullTimeGoalsOverall);
        
        var zeroZeroGames = gameData.ZeroZeroGoal().GetTwoDigitAfterComma();
          
        return new PoissonAverage(
            0,0);
    }

    private static decimal WonGames(IList<GameData> gameData, NextGame nextGame)
    {
        var wonFullTimeGames = gameData.GetPercent(i => i.HomeTeam == nextGame.Team && i.FTR == "H" ||
                                                        i.AwayTeam == nextGame.Team && i.FTR == "A");

        var wonHalfTimeGames = gameData.GetPercent(i => i.HomeTeam == nextGame.Team && i.HTR == "H" ||
                                                        i.AwayTeam == nextGame.Team && i.HTR == "A");

        var wonGames = wonHalfTimeGames.GetWeightedValue(wonFullTimeGames);
        return wonGames;
    }
    
    private static decimal GamesWithMoreThanTwoGoals(IList<GameData> gameData, NextGame nextGame)
    {
        var moreThanTwoGoals = gameData.GetPercent(i => i.HomeTeam == nextGame.Team  ||
                            i.AwayTeam == nextGame.Team && i.HTAG + i.HTHG >= 1 && i.FTAG + i.FTHG > 2);

        return moreThanTwoGoals;
    }
    
    private static decimal GamesWithTwoToThreeGoals(IList<GameData> gameData, NextGame nextGame)
    {
        var moreThanTwoGoals = gameData.GetPercent(i => i.HomeTeam == nextGame.Team  || 
                                                                      i.AwayTeam == nextGame.Team && 
                                                                      i.FTAG + i.FTHG == 3 || i.FTAG + i.FTHG == 2);

        return moreThanTwoGoals;
    }

 

    public static decimal GetWeightedValue(this decimal left, decimal right) => left * 0.40m + right * 0.60m;

    
    internal static IList<GameData> GetGamesDataBy(this IList<GameData> gameData, NextGame nextGame)
    {
        var games = gameData
            .GetMatchesBy(a => a.HomeTeam == nextGame.Team || a.AwayTeam == nextGame.Team);

        if (nextGame.TakeLastGames != 0)
            games = games.Take(nextGame.TakeLastGames).ToList();
        
        return games;
    }

   
    
    internal static string GetTwoDigitAfterComma<T>(this T value) 
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

    private static (decimal halftime, decimal fulltime) AnalyseMoreGoals(this IList<GameData> matches,
        string team,
        bool isHome, 
        bool overall
    )
    {
        var halftimePercentage = matches
            .GetPercent(i => isHome 
                ? i.HomeTeam == team && i.HTHG > i.FTHG || i.HTAG > i.FTAG 
                : i.AwayTeam == team && i.HTAG > i.FTAG || i.HTHG > i.FTHG
            );
        
        var fullTimePercentage = matches
            .GetPercent(i => isHome 
                ? i.HomeTeam == team && i.FTHG > i.HTHG || i.FTAG > i.HTAG
                : i.AwayTeam == team && i.FTAG > i.HTAG || i.FTHG > i.HTHG);
        
        if (overall)
        {
            halftimePercentage = matches
                .GetPercent(i => i.HTHG > i.FTHG || i.HTAG > i.FTAG);
            
            fullTimePercentage = matches
                .GetPercent(i => i.FTAG > i.HTAG || i.FTHG > i.HTHG);
        }
        
        var result = (halftimePercentage.GetTwoDigitAfterComma(), fullTimePercentage.GetTwoDigitAfterComma());

        return result;
    }
    
    private static decimal AnalyseGoals(
        this IList<GameData> matches,
        string team,
        bool isHome, 
        bool overall, 
        int expectedGoal,
        bool halfTimeGoal
    )
    {
        
        var percentage = matches
            .Where(i => isHome ? i.HomeTeam == team : i.AwayTeam == team)
            .GetPercent(p => isHome 
                ? halfTimeGoal ? p.HTHG >= expectedGoal : p.FTHG >= expectedGoal
                : halfTimeGoal ? p.HTAG >= expectedGoal : p.FTAG >= expectedGoal);
        
        if (overall)
        {
            percentage = matches.GetPercent(a => halfTimeGoal 
                ? a.HomeTeam == team && a.HTHG >= expectedGoal || a.AwayTeam == team && a.HTAG >= expectedGoal 
                : a.HomeTeam == team && a.FTHG >= expectedGoal || a.AwayTeam == team && a.FTAG >= expectedGoal);
        }
        
        var result = (percentage.GetTwoDigitAfterComma());

        return result;
    }

    /// <summary>
    /// This is an extension method for the matches type, and it takes two arguments,
    /// a Team object and an int expectedGoal.
    /// This method calculates the average number of games where a team scored a certain number of goals or more.
    /// </summary>
    /// <param name="matches">List of previous matches</param>
    /// <param name="team">team to calculate average</param>
    /// <param name="expectedGoal">expected goal</param>
    /// <returns>Calculated Average</returns>
    public static decimal GetAverageBy(this IEnumerable<GameData> matches, Team team, int expectedGoal)
    {
        var filteredMatches = matches
            .Where(i => i.HomeTeam == team.Name || i.AwayTeam == team.Name)
            .ToList();
        
        var totalGames = filteredMatches.Count;
        var matchesWithGoal = filteredMatches.Count(match => IsMatchWithGoal(match, team, expectedGoal));
        
        var percentage = (decimal)matchesWithGoal / totalGames;
        return Math.Round(percentage, 2);
    }

    /// <summary>
    /// This method checks either the halftime or full time goals for both home and away teams.
    /// The team argument has the overall set true then it will check if team scored a goal either at home or away.
    /// If the team argument halftime property set true then it will check halftime scored
    ///
    /// Otherwise it will check based on the team argument property `IsHome` team scored or not
    /// Finally, it calculates the percentage of games with the expected goals and rounds it to 2 decimal points and returns it.
    /// Overall, this method allows you to filter matches based on a specific team and calculate
    /// the percentage of games where that team scored a certain number of goals or more.
    /// </summary>
    /// <param name="match"></param>
    /// <param name="team"></param>
    /// <param name="expectedGoal"></param>
    /// <returns></returns>
    private static bool IsMatchWithGoal(GameData match, Team team, int expectedGoal)
    {
        if (team.Overall)
        {
            if (team.HalfTimeGoal)
            {
                return match.HomeTeam == team.Name && match.HTHG >= expectedGoal ||
                       match.AwayTeam == team.Name &&  match.HTAG >= expectedGoal;
            }
            
            return match.HomeTeam == team.Name && match.FTHG >= expectedGoal || 
                   match.AwayTeam == team.Name && match.FTAG >= expectedGoal;
        }
        
        var homeHalftimeGoalProperty = team.HalfTimeGoal ? nameof(GameData.HTHG) : nameof(GameData.FTHG);
        var awayHalftimeGoalProperty = team.HalfTimeGoal ? nameof(GameData.HTAG) : nameof(GameData.FTAG);
        var goalProperty = team.IsHome ? homeHalftimeGoalProperty : awayHalftimeGoalProperty;
        var matchGoals = (int)(match.GetType().GetProperty(goalProperty)?.GetValue(match) ?? 0);

        return team.IsHome
            ? match.HomeTeam == team.Name && matchGoals >= expectedGoal
            : match.AwayTeam == team.Name && matchGoals >= expectedGoal;
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
        var found = 0;
        var total = 0;

        var enumerable = source.ToList();
        if (!enumerable.Any())
            return 0;

        foreach (T item in enumerable)
        {
            ++total;
            if (predicate(item))
            {
                found += 1;
            }
        }

        return 100.00m * found / total;
    }
}
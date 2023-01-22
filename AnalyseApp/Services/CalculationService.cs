using System;
using AnalyseApp.Extensions;
using AnalyseApp.models;
using MathNet.Numerics.Distributions;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Transforms;
namespace AnalyseApp.Services;

public class CalculationService
{
    private readonly MLContext _mlContext;
    private readonly List<GameData> _gameData;
    private readonly List<GameData> _upComingMatch;
    private IList<GameData> _currentLeague = new List<GameData>();
    private GameAverage _gameAverage = new ();

    public CalculationService(List<GameData> gameData, List<GameData> upComingMatch)
    {
        _mlContext = new MLContext();
        _gameData = gameData;
        _upComingMatch = upComingMatch;
    }

    public void Execute(string homeTeam, string awayTeam, string league)
    {
        AnalyseTheSeasonPerformanceBy(homeTeam, awayTeam, league);
        //var allGgames = _gameData.GetLeagueSeasonBy(2018, 2022, league);
        //var model = Train(homeTeam, awayTeam);
        //PredictMatch(model);

        /*  var result = new List<MatchProbability>();
        var lastSeasons = AnalyseTheSeasonPerformanceBy(homeTeam, awayTeam, league, 2018, 2022);
        var currentSeason = AnalyseTheSeasonPerformanceBy(homeTeam, awayTeam, league, 2022, 2023); 
        var currentGameBookmakers = GetBet365BookmakersValuesBy(homeTeam, awayTeam);

         var homeBet365 = 1 / 2.0;
         var awayBet365 = 1 / 3.60;
         var drawBet365 = 1 / 3.40;
         var goalGoalBet365 = 1 / 1.90;
         var moreThanTwoGoalBet365 = 1 / 2.10;
         var TwoToThreeGoalBet365 = 1 / 2.05;
         var lessThanThreeGoalBet365 = 1 / 1.72;

        
        var homeBet365 = 1 / 2.45;
        var awayBet365 = 1 / 2.90;
        var drawBet365 = 1 / 3.80;
        var goalGoalBet365 = 1 / 2.00;
        var moreThanTwoGoalBet365 = 1 / 2.37;
        var TwoToThreeGoalBet365 = 1 / 2.00;
        var lessThanThreeGoalBet365 = 1 / 1.57;
        
        var homeBet365 = 1 / 1.40;
        var awayBet365 = 1 / 7.50;
        var drawBet365 = 1 / 5.00;
        var goalGoalBet365 = 1 / 1.80;
        var moreThanTwoGoalBet365 = 1 / 1.61;
        var TwoToThreeGoalBet365 = 1 / 2.00;
        var lessThanThreeGoalBet365 = 1 / 2.30;
        var home = CalculateWeighting(lastSeasons["HomeWin"], currentSeason["HomeWin"]);
        var away = CalculateWeighting(lastSeasons["AwayWin"], currentSeason["AwayWin"]);
        var draw = CalculateWeighting(lastSeasons["Draw"], currentSeason["Draw"]);
        var goalGoal = CalculateWeighting(lastSeasons["BothTeamScore"], currentSeason["BothTeamScore"]);
        var moreThanTwoGoals = CalculateWeighting(lastSeasons["MoreThanTwoGoals"], currentSeason["MoreThanTwoGoals"]);
        var twoToThreeGoal = CalculateWeighting(lastSeasons["TwoToThree"], currentSeason["TwoToThree"]);
        var zeroZeroGoal = CalculateWeighting(lastSeasons["ZeroZeroGoal"], currentSeason["ZeroZeroGoal"]);
        var lessThanThreeGoal = CalculateWeighting(lastSeasons["LessThanTwoGoals"], currentSeason["LessThanTwoGoals"]);
        
        foreach (var allSeason in lastSeasons)
        {
            var currentSeasonProbability = currentSeason
                .Where(i => i.Key == allSeason.Key)
                .Select(ii => ii.Value)
                .FirstOrDefault();

           
              
            result.Add(new MatchProbability
            {
                Key = allSeason.Key,
                Probability = allSeason.Value.CalculateWeighting(currentSeasonProbability),
                Bet365BookMaker = 1 / GetBet365BookMakersProbabilityBy(currentGameBookmakers, allSeason.Key)
            });
            
        }

        return result;
        ;
        //  var homeWin = lastSeason.Values + currentSeason.Values;
        //  var awayWin = awayWinMatches.Sum(i => i.Value[1]);
        //  var draw = drawMatches.Sum(i => i.Value[0]);
*/


    }
    

    private Dictionary<string, double> AnalyseTheSeasonPerformanceBy(
        string homeTeam, string awayTeam, string league)
    {
        var gameAverage = new GameAverage
        {
            CurrentSeason = AnalyseCurrentSeasonBy(homeTeam, awayTeam, league),
            LastSixMatches = AnalyseLastSixGamesBy(homeTeam, awayTeam, league),
            HistoricalMatches = AnalyseAllSeasonBy(homeTeam, awayTeam, league),
        };

        MergeTheAnalysis(gameAverage, homeTeam, awayTeam);
        return null;
    }

    private GameAverage MergeTheAnalysis(GameAverage gameAverage, string homeTeam, string awayTeam)
    {
        var currentLeagueScore = gameAverage.CurrentSeason;
        var lastSixScore = gameAverage.LastSixMatches;
        var allGamesScore = gameAverage.HistoricalMatches;
        
        var homeGameScoredAverage = CalculateWeighting(
            lastSixScore?.ScoredGamesAverage.Home,
            currentLeagueScore?.ScoredGamesAverage.Home,
            allGamesScore?.ScoredGamesAverage.Home
        );
        var awayGameScoredAverage = CalculateWeighting(
            lastSixScore?.ScoredGamesAverage.Away,
            currentLeagueScore?.ScoredGamesAverage.Away,
            allGamesScore?.ScoredGamesAverage.Away
        );
        
        var msg = string.Empty;
        if (allGamesScore?.HeadToHeadGameAverage.TotalGames < 3)
            msg = "CAUTION!";
        
        
        if (homeGameScoredAverage < 70 || awayGameScoredAverage < 70 || allGamesScore?.HeadToHeadGameAverage.BothTeamScore < 50)
        {
            msg = $"{msg} {homeTeam}:{awayTeam} not qualified. {allGamesScore?.HeadToHeadGameAverage.BothTeamScore}";
        }

        var homeGameZeroZeroAverage = CalculateWeighting(
            lastSixScore?.ZeroZeroGameAverage.Home,
            currentLeagueScore?.ZeroZeroGameAverage.Home,
            allGamesScore?.ZeroZeroGameAverage.Home
        );
        var awayGameZeroZeroAverage = CalculateWeighting(
            lastSixScore?.ZeroZeroGameAverage.Away,
            currentLeagueScore?.ZeroZeroGameAverage.Away,
            allGamesScore?.ZeroZeroGameAverage.Away
        );

        if (homeGameZeroZeroAverage > 25 || awayGameZeroZeroAverage > 25 && string.IsNullOrWhiteSpace(msg))
        {
            msg = $"{msg} {homeTeam}:{awayTeam} not qualified. {allGamesScore?.HeadToHeadGameAverage.BothTeamScore}";
        }
        
        var homeGameZeroOneResultAverage = CalculateWeighting(
            lastSixScore?.ZeroOneResult.Home,
            currentLeagueScore?.ZeroOneResult.Home,
            allGamesScore?.ZeroOneResult.Home
        );
        var awayGameZeroOneResultAverage = CalculateWeighting(
            lastSixScore?.ZeroOneResult.Away,
            currentLeagueScore?.ZeroOneResult.Away,
            allGamesScore?.ZeroOneResult.Away
        );
        var homeGameOneSideResultAverage = CalculateWeighting(
            lastSixScore?.OneSideResult.Home,
            currentLeagueScore?.OneSideResult.Home,
            allGamesScore?.OneSideResult.Home
        );
        var awayGameOneSideResultAverage = CalculateWeighting(
            lastSixScore?.OneSideResult.Away,
            currentLeagueScore?.OneSideResult.Away,
            allGamesScore?.OneSideResult.Away
        );

        if (homeGameZeroOneResultAverage > 25 || awayGameZeroOneResultAverage > 25 && string.IsNullOrWhiteSpace(msg) &&
            homeGameOneSideResultAverage > 25 || awayGameOneSideResultAverage > 25)
        {
            msg = $"{msg} {homeTeam}:{awayTeam} not qualified. one side result are bigger than 25%";
        }

        var homeGameHalftimeScoredGamesAverageAverage = CalculateWeighting(
            lastSixScore?.HalftimeScoredGamesAverage.Home,
            currentLeagueScore?.HalftimeScoredGamesAverage.Home,
            allGamesScore?.HalftimeScoredGamesAverage.Home
        );
        var awayGameHalftimeScoredGamesAverageAverage = CalculateWeighting(
            lastSixScore?.HalftimeScoredGamesAverage.Away,
            currentLeagueScore?.HalftimeScoredGamesAverage.Away,
            allGamesScore?.HalftimeScoredGamesAverage.Away
        );
        if (homeGameHalftimeScoredGamesAverageAverage > 25 || awayGameHalftimeScoredGamesAverageAverage > 25 && string.IsNullOrWhiteSpace(msg))
        {
            msg = $"{msg} {homeTeam}:{awayTeam} not qualified. halftime score less than 25%";
        }
        var homeGameMoreThanTwoGoalsAverage = CalculateWeighting(
            lastSixScore?.ScoreThanTwoGoalsAverage.Home,
            currentLeagueScore?.ScoreThanTwoGoalsAverage.Home,
            allGamesScore?.ScoreThanTwoGoalsAverage.Home
        );
        var awayGameMoreThanTwoGoalsAverage = CalculateWeighting(
            lastSixScore?.ScoreThanTwoGoalsAverage.Away,
            currentLeagueScore?.ScoreThanTwoGoalsAverage.Away,
            allGamesScore?.ScoreThanTwoGoalsAverage.Away
        );
        if (homeGameMoreThanTwoGoalsAverage > 50 || awayGameMoreThanTwoGoalsAverage > 50 && string.IsNullOrWhiteSpace(msg))
        {
            msg = $"{msg} {homeTeam}:{awayTeam} not qualified. moore than two goals average is less than 50%";
        }
        
        if (!string.IsNullOrWhiteSpace(msg) )
            Console.WriteLine(msg);
        
        return null;
    }

    private double? CalculateWeighting(double? left, double? middle, double? right) 
        => left * 0.40 + middle * 0.30 + right * 0.30;

    private Average? AnalyseCurrentSeasonBy(string homeTeam, string awayTeam, string league)
    {
        var currentLeague = _gameData.GetLeagueSeasonBy(2022, 2023, league);
        if (!currentLeague.TeamsAreInLeague(homeTeam, awayTeam))
            return null;

        var average = new Average
        {
            ZeroZeroGameAverage =
            {
                Home = TeamZeroZeroGames(currentLeague, homeTeam, atHome: true),
                Away = TeamZeroZeroGames(currentLeague, homeTeam)
            },
            ZeroOneResult =
            {
                Home = TeamOneSideResultGames(currentLeague, homeTeam, true, true),
                Away = TeamOneSideResultGames(currentLeague, homeTeam, true)
            },
            OneSideResult =
            {
                Home = TeamOneSideResultGames(currentLeague, homeTeam, true),
                Away = TeamOneSideResultGames(currentLeague, homeTeam)
            },
            ScoredGamesAverage =
            {
                Home = TeamScoredGameAverage(currentLeague, homeTeam, atHome: true),
                Away = TeamScoredGameAverage(currentLeague, awayTeam)
            },
            HalftimeScoredGamesAverage = 
            {
                Home = TeamScoredGameAverage(currentLeague, homeTeam, halftime: true, atHome: true),
                Away = TeamScoredGameAverage(currentLeague, awayTeam, halftime: true)
            },
            HalftimeScoreAverage =
            {
                Home = TeamScoreAverage(currentLeague, homeTeam, true),
                Away = TeamScoreAverage(currentLeague, awayTeam, true)
            },
            ScoreThanTwoGoalsAverage =
            {
                Home = TeamMoreThanTwoGoalsGames(currentLeague, homeTeam),
                Away = TeamMoreThanTwoGoalsGames(currentLeague, awayTeam)
            },
        };

        return average;
    }
   
    private Average? AnalyseAllSeasonBy(string homeTeam, string awayTeam, string league)
    {
        var currentLeague = _gameData.GetLeagueSeasonBy(2017, 2022, league);
        if (!currentLeague.TeamsAreInLeague(homeTeam, awayTeam))
            return null;

        var average = new Average
        {
            ZeroZeroGameAverage =
            {
                Home = TeamZeroZeroGames(currentLeague, homeTeam, atHome: true),
                Away = TeamZeroZeroGames(currentLeague, homeTeam)
            },
            ZeroOneResult =
            {
                Home = TeamOneSideResultGames(currentLeague, homeTeam, true, true),
                Away = TeamOneSideResultGames(currentLeague, homeTeam, true)
            },
            OneSideResult =
            {
                Home = TeamOneSideResultGames(currentLeague, homeTeam, true),
                Away = TeamOneSideResultGames(currentLeague, homeTeam)
            },
            ScoredGamesAverage =
            {
                Home = TeamScoredGameAverage(currentLeague, homeTeam, atHome: true),
                Away = TeamScoredGameAverage(currentLeague, awayTeam)
            },
            HalftimeScoredGamesAverage = 
            {
                Home = TeamScoredGameAverage(currentLeague, homeTeam, halftime: true, atHome: true),
                Away = TeamScoredGameAverage(currentLeague, awayTeam, halftime: true)
            },
            HalftimeScoreAverage =
            {
                Home = TeamScoreAverage(currentLeague, homeTeam, true),
                Away = TeamScoreAverage(currentLeague, awayTeam, true)
            },
            ScoreThanTwoGoalsAverage =
            {
                Home = TeamMoreThanTwoGoalsGames(currentLeague, homeTeam),
                Away = TeamMoreThanTwoGoalsGames(currentLeague, awayTeam)
            },
            HeadToHeadGameAverage = TeamHeadToHeadGameAverage(currentLeague, homeTeam, awayTeam)
        };

        return average;
    }

    private Average? AnalyseLastSixGamesBy(string homeTeam, string awayTeam, string league)
    {
        var currentLeague = _gameData.GetLeagueSeasonBy(2022, 2023, league)
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam ||
                        i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .Take(12)
            .ToList();
        if (!currentLeague.TeamsAreInLeague(homeTeam, awayTeam))
            return null;
        var average = new Average
        {
            ZeroZeroGameAverage =
            {
                Home = TeamZeroZeroGames(currentLeague, homeTeam, atHome: true),
                Away = TeamZeroZeroGames(currentLeague, homeTeam)
            },
            ZeroOneResult =
            {
                Home = TeamOneSideResultGames(currentLeague, homeTeam, true, true),
                Away = TeamOneSideResultGames(currentLeague, homeTeam, true)
            },
            OneSideResult =
            {
                Home = TeamOneSideResultGames(currentLeague, homeTeam, true),
                Away = TeamOneSideResultGames(currentLeague, homeTeam)
            },
            ScoredGamesAverage =
            {
                Home = TeamScoredGameAverage(currentLeague, homeTeam, atHome: true),
                Away = TeamScoredGameAverage(currentLeague, awayTeam)
            },
            HalftimeScoredGamesAverage = 
            {
                Home = TeamScoredGameAverage(currentLeague, homeTeam, halftime: true, atHome: true),
                Away = TeamScoredGameAverage(currentLeague, awayTeam, halftime: true)
            },
            HalftimeScoreAverage =
            {
                Home = TeamScoreAverage(currentLeague, homeTeam, true),
                Away = TeamScoreAverage(currentLeague, awayTeam, true)
            },
            ScoreThanTwoGoalsAverage = 
            {
                Home = TeamMoreThanTwoGoalsGames(currentLeague, homeTeam),
                Away = TeamMoreThanTwoGoalsGames(currentLeague, awayTeam)
            }
        };

        return average;
    }

    /// <summary>
    /// The method is used to check if the team has the ability to score at least one goal
    /// in the majority of its home and away games.
    /// the method returns a boolean value indicating if the homeScoreGameAverage and awayScoreGameAverage are greater
    /// than 0.60, which means the team has the ability to score at least one goal in more than 60% of its games.
    /// </summary>
    /// <param name="leagueSeason"></param>
    /// <param name="team"></param>
    private double TeamScoredGameAverage(IList<GameData> leagueSeason, string team, bool halftime = false, bool atHome = false)
    {
        var homeTeamHomeField = leagueSeason
            .Where(i => i.HomeTeam == team)
            .ToList();
        
        var homeTeamAwayField = leagueSeason
            .Where(i => i.AwayTeam == team)
            .ToList();


        var homeScoreGames = (double)homeTeamHomeField.Count(i => halftime ? i.HTHG  > 0 : i.FTHG > 0);
        var awayScoreGames = (double)homeTeamAwayField.Count(i => halftime ? i.HTAG  > 0 : i.FTAG > 0);
        
        var homeScoreGameAverage = homeScoreGames.Divide(homeTeamHomeField.Count);
        var awayScoreGameAverage = awayScoreGames.Divide(homeTeamAwayField.Count);

        var scoreAverage = homeScoreGameAverage.CalculateWeighting(awayScoreGameAverage, atHome ? 60 : 40);
        return  Math.Round(scoreAverage, 3);
    }
    
    private double TeamZeroZeroGames(IList<GameData> leagueSeason, string team, bool atHome = false)
    {
        var homeTeamHomeField = leagueSeason.Where(i => i.HomeTeam == team).ToList();
        var homeTeamAwayField = leagueSeason.Where(i => i.AwayTeam == team).ToList();

        var homeScoreGames = (double)homeTeamHomeField.Count(i => i.FTHG == 0);
        var awayScoreGames = (double)homeTeamAwayField.Count(i => i.FTAG == 0);
        
        var homeScoreGameAverage = homeScoreGames.Divide(homeTeamHomeField.Count);
        var awayScoreGameAverage = awayScoreGames.Divide(homeTeamAwayField.Count);

        var scoreAverage = homeScoreGameAverage
            .CalculateWeighting(awayScoreGameAverage, atHome ? 60 : 40);

        return Math.Round(scoreAverage, 3);
    }
    
    private double TeamOneSideResultGames(IList<GameData> leagueSeason, string team, bool atHome = false, bool zeroOneResult = false)
    {
        var homeTeamHomeField = leagueSeason.Where(i => i.HomeTeam == team).ToList();
        var homeTeamAwayField = leagueSeason.Where(i => i.AwayTeam == team).ToList();

        var homeScoreGames = (double)homeTeamHomeField.Count(i => zeroOneResult 
            ? i is { FTHG: >= 1 and < 3, FTAG: 0 }
            : i is { FTAG: >= 1 and < 3, FTHG: 0 });
        var awayScoreGames = (double)homeTeamAwayField.Count(i => zeroOneResult 
            ? i is { FTAG: >= 1 and < 3, FTHG: 0 }
            : i is { FTHG: >= 1 and < 3, FTAG: 0 });
        
        var homeScoreGameAverage = homeScoreGames.Divide(homeTeamHomeField.Count);
        var awayScoreGameAverage = awayScoreGames.Divide(homeTeamAwayField.Count);

        var scoreAverage = homeScoreGameAverage
            .CalculateWeighting(awayScoreGameAverage, atHome ? 60 : 40);

        return Math.Round(scoreAverage, 3);
    }
  
    private double TeamMoreThanTwoGoalsGames(IList<GameData> leagueSeason, string team, bool atHome = false)
    {
        var homeTeamHomeField = leagueSeason.Where(i => i.HomeTeam == team).ToList();
        var homeTeamAwayField = leagueSeason.Where(i => i.AwayTeam == team).ToList();

        var homeScoreGames = (double)homeTeamHomeField.Count(i => i.FTHG + i.FTAG > 2);
        var awayScoreGames = (double)homeTeamAwayField.Count(i => i.FTHG + i.FTAG > 2);
        
        var homeScoreGameAverage = homeScoreGames.Divide(homeTeamHomeField.Count);
        var awayScoreGameAverage = awayScoreGames.Divide(homeTeamAwayField.Count);

        var scoreAverage = homeScoreGameAverage
            .CalculateWeighting(awayScoreGameAverage, atHome ? 60 : 40);

        return Math.Round(scoreAverage, 3);
    }
    private static double TeamScoreAverage(IList<GameData> leagueSeason, string team, bool halftime = false)
    {
        var teamHomeField = leagueSeason
            .Where(i => i.HomeTeam == team || i.AwayTeam == team)
            .Select(ii => halftime ? ii.HTHG : ii.FTHG)
            .Average() ?? 0.0;
        
        return Math.Round(teamHomeField, 3);
    }

    private static HeadToHead TeamHeadToHeadGameAverage(IList<GameData> leagueSeason, string homeTeam, string awayTeam)
    {
        var teamHomeField = leagueSeason
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam)
            .ToList();

        if (teamHomeField.Count == 0)
            return new HeadToHead();

        var home = teamHomeField.Average(i => i.FTHG ?? 0);
        var away = teamHomeField.Average(i => i.FTAG ?? 0);
        
        var homeHalftime = teamHomeField.Average(i => i.HTHG ?? 0);
        var awayHalftime = teamHomeField.Average(i => i.HTAG ?? 0);
        var headToHead = new HeadToHead
        {
            BothTeamScore = teamHomeField.Count(i => i is { FTHG: >= 1, FTAG: >= 1 }).Divide(teamHomeField.Count),
            MoreThanTwoGoals = teamHomeField.Count(i => i.FTHG + i.FTAG > 2).Divide(teamHomeField.Count),
            TwoToThreeGoals = teamHomeField.Count(i => i.FTHG + i.FTAG == 2 || i.FTHG + i.FTAG == 3)
                .Divide(teamHomeField.Count),
            NoGoal = teamHomeField.Count(i => i is { FTHG: 0, FTAG: 0 }).Divide(teamHomeField.Count),
            TotalGames = teamHomeField.Count,
            HalfTimeScored = teamHomeField.Count(i => i is { HTHG: 1, HTAG: 1 }).Divide(teamHomeField.Count),
            MoreThanTwoGoalAverage = home.CalculateWeighting(away, 40),
            HalftimeScoreAverage = homeHalftime.CalculateWeighting(awayHalftime, 45)
        };

        return headToHead;
    }
    
}
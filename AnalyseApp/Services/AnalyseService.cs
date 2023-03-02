using System.Globalization;
using AnalyseApp.Commons.Constants;
using AnalyseApp.Extensions;
using AnalyseApp.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace AnalyseApp.Services;

public class AnalyseService
{
    private const string FileDir = "C:\\shivm\\AnalyseApp\\data";
    private List<HistoricalGame> _historicalGames = new();
    private List<HistoricalGame> _upComingGames = new();
    private CalculateService _calculateService;
    
    private bool oneSideScoreSuggestLessThanThreeGoal = false;
    private bool oneSideScoreTwoToThreeGoal = false;
    private bool oneSideScoreSuggestMoreThanTwoGoal = false;
    

    public AnalyseService()
    {
        _calculateService = new CalculateService(_historicalGames);
    }
    internal AnalyseService ReadHistoricalGames()
    {
        var files = Directory.GetFiles($"{FileDir}\\raw_csv");

        foreach (var file in files)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, config);

            var currentFileGames = csv.GetRecords<HistoricalGame>();
            _historicalGames.AddRange(currentFileGames);
        }

        _historicalGames = _historicalGames
            .OrderByDescending(i => DateTime.Parse(i.Date)).ToList();
        return this;
    }
    

    internal AnalyseService ReadUpcomingGames()
    {
        var files = Directory.GetFiles($"{FileDir}\\upcoming_matches");
        foreach (var file in files)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, config);

            var currentFileGames = csv.GetRecords<HistoricalGame>();

            _upComingGames.AddRange(currentFileGames);
        }

        _upComingGames = _upComingGames.OrderByDescending(i => i.Date).ToList();

        return this;
    }

    internal void AnalyseGames()
    {
        var count = 0;
        var list = new List<string>();
        Console.WriteLine(_upComingGames.Count);
        foreach (var comingGame in _upComingGames)
        {
            if (comingGame.HomeTeam == "Inter" || 
                comingGame.HomeTeam == "Wolves" ||
                comingGame.HomeTeam == "Huddersfield" ||
                comingGame.HomeTeam == "Middlesbrough" ||
                comingGame.AwayTeam == "Bristol City")
            {
                            
            }
            
            var lastSixGames = GetLastSixGamesAnalysis(comingGame);
            var allGames = GetAllGamesAnalysis(comingGame);
            var headToHeads = GetHeadToHeadAnalysis(comingGame);

             var filterDangersGames = FilterDangersGames(lastSixGames, allGames, headToHeads);
             if (filterDangersGames.qualified)
             {
                 var averageQualification = FilterAverageQualification(lastSixGames, allGames, headToHeads);
                 if (averageQualification)
                 {
                     var probability = GetPoisonProbability(comingGame.HomeTeam, comingGame.AwayTeam, comingGame.Div);
                    
                     if ( probability.Value > 0.60)
                     {
                         Console.WriteLine($"{comingGame.Date} {comingGame.HomeTeam}:{comingGame.AwayTeam} {probability.Key} {probability.Value}%");
                     }
                 } 
              }
             else
             {
               //  list.Add($"{comingGame.Date} {comingGame.HomeTeam}:{comingGame.AwayTeam} {filterDangersGames.msg}");
             }
             
           
        }
        
        Console.WriteLine(count);
        list.ForEach(i => Console.WriteLine($"{i}\t"));
    }

    private KeyValuePair<string, double> GetPoisonProbability(string homeTeam, string awayTeam, string league)
    {
        var service = new PoissonService(_historicalGames);
        var probability = service.AnalysePerformance(homeTeam, awayTeam, league, _historicalGames)
            .FirstOrDefault();

        return probability;
    }

    private static bool FilterAverageQualification(
        (GameData Home, GameData Away) lastSixGames, 
        (GameData Home, GameData Away) allGames, 
        HeadToHeadData headToHeads)
    {
        if (allGames.Home.GoalsGameAverage > 0.60 && allGames.Away.GoalsGameAverage > 0.60 &&
            lastSixGames.Home.LastThreeGamesScored && lastSixGames.Away.LastThreeGamesScored) 
            return true;

        return lastSixGames.Home.ThreeGamesScored && lastSixGames.Away.ThreeGamesScored && headToHeads.LastFourBothScored;
    }

    private (bool qualified, string msg) FilterDangersGames(
        (GameData Home, GameData Away) lastSixGames,
        (GameData Home, GameData Away) allGames,
        HeadToHeadData headToHeads
    )
    {
        if (lastSixGames.Home.ZeroZeroGameAverage > 0.50 || lastSixGames.Away.ZeroZeroGameAverage > 0.50) 
            return (false, $"failed because of last 6 games 0:0 {lastSixGames.Home.ZeroZeroGameAverage} {lastSixGames.Away.ZeroZeroGameAverage} ");

        if (allGames.Home.ZeroZeroGameAverage > 0.40 || allGames.Away.ZeroZeroGameAverage > 0.40)
            return (false, $"failed because of last 6 season 0:0 {allGames.Home.ZeroZeroGameAverage} {allGames.Away.ZeroZeroGameAverage}");

        if (lastSixGames.Home.ZeroOneGameAverage > 0.50 || lastSixGames.Away.ZeroOneGameAverage > 0.50)
           return (false, $"failed because of last 6 games 0:1 {lastSixGames.Home.ZeroOneGameAverage} {lastSixGames.Away.ZeroOneGameAverage}");
        
        if (allGames.Home.ZeroOneGameAverage > 0.40 || allGames.Away.ZeroOneGameAverage > 0.40)
            return (false, $"failed because of last 6 season 0:1 {allGames.Home.ZeroOneGameAverage} {allGames.Away.ZeroOneGameAverage}");
        
        if (headToHeads is { ThreeZeroZeroGames: false, ThreeZeroOneGames: false })
            return (true, "");
        
        return (false, $"failed because of head to head count: {headToHeads.MatchesPlayed} 0:0 {headToHeads.ZeroZeroGameAverage} 0:1  {headToHeads.ZeroOneHomeGameAverage} {headToHeads.ZeroOneAwayGameAverage}");

    }

    private (GameData Home, GameData Away) GetLastSixGamesAnalysis(HistoricalGame comingGame)
    {
        // Query the last 6 games each by each team and merge them together
        var homeGames = _historicalGames.GetLastSixGamesBy(comingGame.HomeTeam);
        var awayGames = _historicalGames.GetLastSixGamesBy(comingGame.AwayTeam);
       
        // Calculate The metrics and create the game object
        var homeData = GetTeamData(homeGames, comingGame.HomeTeam);
        var awayData = GetTeamData(awayGames, comingGame.AwayTeam);
        
        return (homeData, awayData);
    }

    private (GameData Home, GameData Away) GetAllGamesAnalysis(HistoricalGame comingGame)
    {
        // Query all games each by each team 
        var homeGames = _historicalGames.GetAllSeasonGamesBy(comingGame.HomeTeam);
        var awayGames = _historicalGames.GetAllSeasonGamesBy(comingGame.AwayTeam);
        
        // Calculate The metrics and create the game object
        var homeData = GetTeamData(homeGames, comingGame.HomeTeam);
        var awayData = GetTeamData(awayGames, comingGame.AwayTeam);

        return (homeData, awayData);
    }
    
    private HeadToHeadData GetHeadToHeadAnalysis(HistoricalGame comingGame)
    {
        var headToHeads = _historicalGames
            .GetHeadToHeadGamesBy(comingGame.HomeTeam, comingGame.AwayTeam)
            .OrderByDescending(i => DateTime.Parse(i.Date))
            .ToList();

        var headToHead = new HeadToHeadData
        {
            HomeTeam = comingGame.HomeTeam,
            AwayTeam = comingGame.AwayTeam,
            MatchesPlayed = headToHeads.Count,
            ZeroZeroGameAverage = headToHeads.CalculateZeroZeroAccuracy(),
            HomeGoalsGameAverage = headToHeads.CalculateScoreGameAccuracy(comingGame.HomeTeam),
            AwayGoalsGameAverage = headToHeads.CalculateScoreGameAccuracy(comingGame.AwayTeam),
            ZeroOneHomeGameAverage = headToHeads.CalculateNoScoreGamesAccuracyBy(comingGame.HomeTeam, 1),
            ZeroOneAwayGameAverage = headToHeads.CalculateNoScoreGamesAccuracyBy(comingGame.AwayTeam, 1),
            HomeHalftimeGoalsScored = headToHeads.CalculateHalftimeScoreGamesAccuracy(comingGame.HomeTeam),
            AwayHalftimeGoalsScored = headToHeads.CalculateHalftimeScoreGamesAccuracy(comingGame.AwayTeam),
            ThreeZeroZeroGames = headToHeads.Take(3).All(i => i is { FTAG: 0, FTHG:  0 }),
            ThreeZeroOneGames = headToHeads.Take(3).All(i => i is { FTAG: > 0, FTHG:  0 } or { FTAG: 0, FTHG: > 0 }),
            LastFourBothScored = headToHeads.Take(4).All(i => i is { FTAG: > 0, FTHG: > 0 }),
            LastFourOverGames = headToHeads.Take(4).All(i => i.FTAG + i.FTHG > 2)
        };
       
        return headToHead;
    }

    private GameData GetTeamData(List<HistoricalGame> games, string team)
    {
        var gameData = new GameData
        {
            Name = team,
            ZeroZeroGameAverage = games.CalculateZeroZeroAccuracy(),
            ZeroOneGameAverageByTeam = games.CalculateNoScoreGamesAccuracyBy(team, 1),
            ZeroOneGameAverage = games.CalculateNoScoreGamesAccuracy(1),
            HalftimeGoalAverage = games.CalculateHalftimeScoreGamesAccuracy(team),
            GoalsGameAverage = games.CalculateScoreGameAccuracy(team),
            GoalsScored = games.CalculateScoredGoalAccuracy(team),
            GoalsConceded = games.CalculateConcededGoalAccuracy(team),
            LastThreeGamesScored = games.Count(i => i.HomeTeam == team && i.FTHG > 0 ||i.AwayTeam ==  team && i.FTAG > 0) > 3,
            ThreeGamesScored = games.Count(i => i.HomeTeam == team && i.FTHG > 0 ||i.AwayTeam ==  team && i.FTAG > 0) == 3,
            MarkovChainProbability = _calculateService.TeamMarkovChainProbability(games, team)
        };
        
        return gameData;
    }

    
    private static (bool Qualified,double probability, string indicator) BothTeamScore(
        GameQualification lastSixGames, 
        GameQualification allGames,
        GameQualification headToHeads)
    {
        var poisson = FindHighestProbability(lastSixGames, allGames, headToHeads);

        if (poisson.Contains("BothTeamScore"))
        {
            
        }
        return (false, 0.0, "test");
    }

    private static (double Average, bool Quailified, string Indicator) ZeroZeroFilterBy(
        GameQualification lastSixGames, 
        GameQualification allGames,
        GameQualification headToHeads
    )
    {
        var homeZeroZeroAverage = lastSixGames.Home.ZeroZeroGameAverage;
        var awayZeroZeroAverage = lastSixGames.Away.ZeroZeroGameAverage;
        
        var homeAllGamesZeroZeroAverage = allGames.Home.ZeroZeroGameAverage;
        var awayAllGamesZeroZeroAverage = allGames.Away.ZeroZeroGameAverage;

        var homeAverage = homeZeroZeroAverage * 0.70 + homeAllGamesZeroZeroAverage * 0.30;
        var awayAverage = awayZeroZeroAverage * 0.70 + awayAllGamesZeroZeroAverage * 0.30;


        var howCouldBe00 = homeZeroZeroAverage > 0.25
            ? $"Home: {GetPercentage(homeZeroZeroAverage)}%"
            : awayZeroZeroAverage > 0.25 
                ? $"Away: {GetPercentage(awayZeroZeroAverage)}%" 
                : "";
        
        if (homeZeroZeroAverage < 0.25 && awayZeroZeroAverage < 0.25)
        {
            var headToHead = GetPercentage(headToHeads.Home.ZeroZeroGameAverage);
            // Head to Head for home and away must be same
            if (headToHeads.Home is { ZeroZeroGameAverage: < 0.30, GamePlayed: >= 3 })
            {
                return (homeAverage * awayAverage, true, "");
            }

            return headToHeads.Home.GamePlayed <= 3 
                ? (homeAverage * awayAverage, false, $"H2H Failed: 0:0 played game are less than 4: {headToHeads.Home.GamePlayed}") 
                : (homeAverage * awayAverage, false, $"H2H Failed: 0:0 {headToHead}% in last {headToHeads.Home.GamePlayed}");
        }

        return (homeAverage * awayAverage, false, $"Could be 0:0 game because in last six games {howCouldBe00}");
    }
    
    private static (double Average, bool Quailified, string Indicator) NoGoalFilterBy(
        GameQualification lastSixGames, 
        GameQualification headToHeads
    )
    {
        var homeNoGoalScored = lastSixGames.Home.NoGoalScoredByTeamAverage;
        var awayNoGoalScored = lastSixGames.Away.NoGoalScoredByTeamAverage;
        
        var howCouldBe = homeNoGoalScored > 0.34
            ? $"Home: {GetPercentage(homeNoGoalScored)}%"
            : awayNoGoalScored > 0.34 
                ? $"Away: {GetPercentage(awayNoGoalScored)}%" 
                : "";
        
        if (homeNoGoalScored < 0.34 && awayNoGoalScored < 0.34 ||
            homeNoGoalScored < 0.70 && lastSixGames.Home.LastThreeGamesWithoutGoal && awayNoGoalScored < 0.34 ||
            awayNoGoalScored < 0.70 && lastSixGames.Away.LastThreeGamesWithoutGoal && homeNoGoalScored < 0.34)
        {
            var headToHead = GetPercentage(headToHeads.Home.NoGoalScoredByTeamAverage);
            var awayHeadToHead = GetPercentage(headToHeads.Away.NoGoalScoredByTeamAverage);
            if (headToHeads.Home.GamePlayed >= 3)
            {
                if (headToHeads is { Home.NoGoalScoredByTeamAverage: < 0.34, Away.NoGoalScoredByTeamAverage: < 0.34 })
                    return (homeNoGoalScored * awayNoGoalScored, true, "");
                
                return (homeNoGoalScored * awayNoGoalScored, false, $"H2H Failed: one side home: {headToHead}% away: {awayHeadToHead} in last {headToHeads.Home.GamePlayed}");
            }

            return (homeNoGoalScored * awayNoGoalScored, false,
                    $"H2H Failed: one side played game are less than 4: {headToHeads.Home.GamePlayed}");
        }

        return (homeNoGoalScored * awayNoGoalScored, false, $"Could be one side game because in last six games {howCouldBe}");
    }

    private static double GetPercentage(double value) =>  Math.Round(value, 2) * 100;

    private static string FindHighestProbability(GameQualification lastSixGames, GameQualification allGames, GameQualification headToHeads)
    {
        var scenarios = new [] { "BothTeamScore", "MoreThanTwoGoals", "LessThanThreeGoals", "TwoToThreeGoals" };
        var probabilities = new Dictionary<string, Func<GameQualification, double>>
        {
            {"BothTeamScore", x => x.PoissonBothScoreProbability},
            {"MoreThanTwoGoals", x => x.PoissonMoreThanTwoGoalsProbability},
            {"LessThanThreeGoals", x => x.PoissonLessThanThreeGoalsProbability},
            {"TwoToThreeGoals", x => x.PoissonTwoToThreeGoalsProbability}
        };

        var highestScenario = scenarios.FirstOrDefault(s =>
            probabilities[s](lastSixGames) > probabilities[s](allGames) &&
            probabilities[s](lastSixGames) > probabilities[s](headToHeads) &&
            probabilities[s](allGames) > probabilities[s](headToHeads)
        );
        if (highestScenario != null)
        {
            return highestScenario;
        }
        else
        {
            var scenariosWithHighestProbability = scenarios
                .OrderByDescending(s => Math.Max(probabilities[s](lastSixGames), Math.Max(probabilities[s](allGames), probabilities[s](headToHeads))))
                .Take(2)
                .ToArray();

            return scenariosWithHighestProbability.Length == 2 ? string.Join(", ", scenariosWithHighestProbability) : "No prediction available";
        }
    }
    
    private static (bool Qualified,double probability, string Indicator) MoreThanTwoScores(
        TeamData homeTeamCurrentData, 
        TeamData awayTeamCurrentData,
        HeadToHead headToHeads,
        IEnumerable<PoissonProbability> probabilities)
    {
        var moreThanTwoScores = probabilities
            .FirstOrDefault(s => s.Key == "MoreThanTwoGoals")?.Probability ?? 0;

        if (headToHeads.MoreThanTwoScored < 0.60 || headToHeads.GamesPlayed < 2 ||  moreThanTwoScores < 0.60) 
            return (false, moreThanTwoScores, "Failed");

        if (homeTeamCurrentData.OneGoalQualified && awayTeamCurrentData.OneGoalQualified && 
            homeTeamCurrentData.GoalAverage > 0.70 && awayTeamCurrentData.GoalAverage > 0.70)
            return (true, moreThanTwoScores, "Qualified");

        return homeTeamCurrentData switch
        {
            { OneGoalQualified: false, ConcededQualified: true } when awayTeamCurrentData is
            {
                OneGoalQualified: true, ConcededQualified: true
            } => (true, moreThanTwoScores, "Dangers Home failed"),
            { OneGoalQualified: true, ConcededQualified: true } when awayTeamCurrentData is
            {
                OneGoalQualified: false, ConcededQualified: true
            } => (true, moreThanTwoScores, "Dangers Away failed"),
            _ => (false, moreThanTwoScores, "Failed")
        };
    }
    
    private static (bool Qualified,double probability, string Indicator) TwoToThreeGoals(
        TeamData homeTeamCurrentData, 
        TeamData awayTeamCurrentData,
        HeadToHead headToHeads,
        IEnumerable<PoissonProbability> probabilities)
    {
        var twoToThreeGoals = probabilities
            .FirstOrDefault(s => s.Key == "TwoToThree")?.Probability ?? 0;

        if (headToHeads.TwoToThreeScored < 0.60 || headToHeads.GamesPlayed < 2 ||  twoToThreeGoals < 0.60) 
            return (false, twoToThreeGoals, "Failed");

        if ((homeTeamCurrentData.OneGoalQualified || awayTeamCurrentData.OneGoalQualified) && 
            homeTeamCurrentData.GoalAverage is < 0.70 and > 0.55 && awayTeamCurrentData.GoalAverage is < 0.70 and > 0.55)
            return (true, twoToThreeGoals, "Qualified");

        return (false, twoToThreeGoals, "Failed");
    }

    /// <summary>
    /// The method check the 0:0 games.
    /// The method retrieve first the zero zero probability of poison.
    /// After that it will check each team Data individually the 0:0 games average are bigger than 20% if so it will
    /// return true otherwise false
    /// </summary>
    /// <param name="homeTeamCurrentData"></param>
    /// <param name="homeTeamData"></param>
    /// <param name="awayTeamCurrentData"></param>
    /// <param name="awayTeamData"></param>
    /// <param name="headToHeads"></param>
    /// <param name="probabilities"></param>
    /// <returns></returns>
    private static bool IsZeroZero(
        TeamData homeTeamCurrentData, 
        //TeamData homeTeamData, 
        TeamData awayTeamCurrentData,
        //TeamData awayTeamData, 
        HeadToHead headToHeads,
        IEnumerable<PoissonProbability> probabilities)
    {
        var zeroZero = probabilities.FirstOrDefault(s => s.Key == "ZeroZeroGoals")?.Probability ?? 0;
        
        return homeTeamCurrentData is { LastAwayGameZeroZero: false, LastHomeGameZeroZero: false } && 
               awayTeamCurrentData is { LastAwayGameZeroZero: false, LastHomeGameZeroZero: false } &&
               zeroZero > 0.20 && headToHeads.BothTeamScored > 0.25;
    }


    private static string GetTicketName(int ticketNr)
    {
        return ticketNr switch
        {
            0 => "Super",
            1 => "Best",
            2 => "Possible",
            3 => "Dangers",
            _ => "Anything is possible"
        };
    }

    private static bool IsDerbyMatch(string title)
    {
        return DerbyTeams.GetDerbyMatches().Contains(title);
    }


    private NextGame LastEightGames(string homeTeam, string awayTeam)
    {
        var currentMatches = _historicalGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == homeTeam ||
                                   i.HomeTeam == homeTeam || i.AwayTeam == awayTeam)
            .GetGameDataBy(2022, 2023);

        var lastTenHomeGames = currentMatches
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .Take(10)
            .ToList();

        var lastTenAwayGames = currentMatches
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .Take(10)
            .ToList();

        if (!lastTenHomeGames.Any() || !lastTenAwayGames.Any())
            return new NextGame();

        var lastFourHomeHomeMatches = lastTenHomeGames
            .Where(i => i.HomeTeam == homeTeam).Take(4).ToList();

        var lastFourHomeAwayMatches = lastTenHomeGames
            .Where(i => i.AwayTeam == homeTeam).Take(4).ToList();

        var lastFourAwayHomeMatches = lastTenAwayGames
            .Where(i => i.HomeTeam == awayTeam).Take(4).ToList();

        var lastFourAwayAwayMatches = lastTenAwayGames
            .Where(i => i.AwayTeam == awayTeam).Take(4).ToList();

        var homeAway = GetTeamDataBy(lastFourHomeAwayMatches, homeTeam);
        var home = GetTeamDataBy(lastFourHomeHomeMatches, homeTeam);

        var awayHome = GetTeamDataBy(lastFourAwayHomeMatches, awayTeam);
        var away = GetTeamDataBy(lastFourAwayAwayMatches, awayTeam);

        var homeScored = GetGoalSumBy(lastTenHomeGames, homeTeam);
        var homeConceded = GetGoalSumBy(lastTenHomeGames, homeTeam, true);
        var homeAllGameCount = lastTenHomeGames.Count;

        var awayScored = GetGoalSumBy(lastTenAwayGames, awayTeam);
        var awayConceded = GetGoalSumBy(lastTenAwayGames, awayTeam, true);
        var awayAllGameCount = lastTenAwayGames.Count;

        var result = new NextGame
        {
            Home = home with
            {
                // Last ten games win accuracy
                LastTenGamesWinAccuracy = lastTenHomeGames.GetWinGamesCountBy(homeTeam).Divide(lastTenHomeGames.Count),

                // Last ten games over accuracy
                LastTenGamesOverTwoGoalsAccuracy = lastTenHomeGames
                    .GetMoreThanTwoGoalScoredGamesCount().Divide(lastTenHomeGames.Count),

                AllowGoals = homeConceded.Divide(homeAllGameCount),
                ScoredGoal = homeScored.Divide(homeAllGameCount),

                // Last ten games draw accuracy
                LastTenGamesDrawAccuracy = lastTenHomeGames
                    .Count(i => i.FTR == "D").Divide(lastTenHomeGames.Count),

                // Last five games in row over
                LastFiveGamesOver = lastTenHomeGames.Take(5).All(i => i.FTAG + i.FTHG > 2),
                LastTwoGamesWithZeroGoal = lastTenHomeGames.Take(1).Any(i => i.FTAG + i.FTHG <= 1),
                // Last five games in row both score
                LastSixGamesBothScored = lastTenHomeGames.Take(6).All(i => i is { FTAG: > 0, FTHG: > 0 }),
                LastFiveGamesLess = lastTenHomeGames.Take(5).All(i => i.FTHG + i.FTAG < 3),
                LastTwoGamesLessThanTwoGoals = lastTenHomeGames.Take(2).Any(i =>
                    (i.FTAG is 1 or 2 && i.FTHG == 0) || (i.FTHG is 1 or 2 && i.FTAG == 0)),
                BothScoreGames = home.BothScoreGames * 0.5 + homeAway.BothScoreGames * 0.5,
                MoreThanTwoGoalsGames = home.MoreThanTwoGoalsGames * 0.5 + homeAway.MoreThanTwoGoalsGames * 0.5,
                HalftimeGoalGames = home.HalftimeGoalGames * 0.5 + homeAway.HalftimeGoalGames * 0.5,
                TwoToThreeGoalGames = home.TwoToThreeGoalGames * 0.5 + homeAway.TwoToThreeGoalGames * 0.5,
                LessThanThreeGoalsAccuracy = home.LessThanThreeGoalsAccuracy * 0.5 + homeAway.LessThanThreeGoalsAccuracy * 0.5
            },
            Away = away with
            {
                // Last ten games win accuracy
                LastTenGamesWinAccuracy = lastTenAwayGames.GetWinGamesCountBy(awayTeam).Divide(lastTenAwayGames.Count),

                // Last ten games over accuracy
                LastTenGamesOverTwoGoalsAccuracy = lastTenAwayGames
                .GetMoreThanTwoGoalScoredGamesCount().Divide(lastTenAwayGames.Count),

                AllowGoals = awayConceded.Divide(awayAllGameCount),
                ScoredGoal = awayScored.Divide(awayAllGameCount),

                // Last ten games draw accuracy
                LastTenGamesDrawAccuracy = lastTenAwayGames
                .Count(i => i.FTR == "D").Divide(lastTenAwayGames.Count),

                // Last five games in row over
                LastFiveGamesOver = lastTenAwayGames.Take(5).All(i => i.FTAG + i.FTHG > 2),

                // Last five games in row both score
                LastSixGamesBothScored = lastTenAwayGames.Take(6).All(i => i is { FTAG: > 0, FTHG: > 0 }),
                LastFiveGamesLess = lastTenAwayGames.Take(5).All(i => i.FTHG + i.FTAG < 3),
                LastTwoGamesWithZeroGoal = lastFourAwayHomeMatches.Take(3).Any(i => i.FTHG < 1) &&
                                                    lastFourAwayAwayMatches.Take(3).Any(i => i.FTAG < 1),
                LastTwoGamesLessThanTwoGoals = lastTenAwayGames.Take(6).Any(i =>
                    (i.FTAG is 1 or 2 && i.FTHG == 0) || (i.FTHG is 1 or 2 && i.FTAG == 0)),
                BothScoreGames = away.BothScoreGames * 0.5 + awayHome.BothScoreGames * 0.5,
                MoreThanTwoGoalsGames = away.MoreThanTwoGoalsGames * 0.5 + awayHome.MoreThanTwoGoalsGames * 0.5,
                HalftimeGoalGames = away.HalftimeGoalGames * 0.5 + awayHome.HalftimeGoalGames * 0.5,
                TwoToThreeGoalGames = away.TwoToThreeGoalGames * 0.5 + awayHome.TwoToThreeGoalGames * 0.5,
                LessThanThreeGoalsAccuracy = away.LessThanThreeGoalsAccuracy * 0.5 + awayHome.LessThanThreeGoalsAccuracy * 0.5
            }
        };

        return result;

    }

    private NextGame LastSixSeason(string homeTeam, string awayTeam)
    {
        var homeGames = _historicalGames
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();

        var awayGames = _historicalGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();

        var headToHeadGames = _historicalGames
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                                i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .ToList();


        var homeScored = GetGoalSumBy(homeGames, homeTeam);
        var homeConceded = GetGoalSumBy(homeGames, homeTeam, true);
        var homeAllGameCount = homeGames.Count;

        var awayScored = GetGoalSumBy(awayGames, awayTeam);
        var awayConceded = GetGoalSumBy(awayGames, awayTeam, true);
        var awayAllGameCount = awayGames.Count;

        var homeTeamData = GetTeamDataBy(homeGames, homeTeam);
        var awayTeamData = GetTeamDataBy(awayGames, awayTeam);
        var headToHead = GetHeadToHeadDataBy(headToHeadGames);

        var result = new NextGame
        {
            Home = homeTeamData with
            {
                ScoredGoal = homeScored.Divide(homeAllGameCount),
                AllowGoals = homeConceded.Divide(homeAllGameCount)
            },
            Away = awayTeamData with
            {
                ScoredGoal = awayScored.Divide(awayAllGameCount),
                AllowGoals = awayConceded.Divide(awayAllGameCount)
            },
            HeadToHead = headToHead

        };

        return result;
    }

    private static Team GetTeamDataBy(IList<HistoricalGame> games, string team)
    {
        var gamesPlayed = games.Count;
        var teamData = new Team
        {
            GamesPlayed = games.Count,
            HalftimeGoalsScored = games.GetHalftimeGoalScoredSumBy(team).Divide(gamesPlayed),
            HalftimeGoalsConceded = games.GetHalftimeGoalConcededSumBy(team).Divide(gamesPlayed),
            // -------------------------------------------------------------------
            NoGoalGames = games.GetNoGoalGameCount().Divide(gamesPlayed),
            OneSideGoalGames = games.GetOneSideGoalGamesCount().Divide(gamesPlayed),
            HalftimeGoalGames = games.GetHalftimeGoalGamesCount().Divide(gamesPlayed),
            BothScoreGames = games.GetBothScoredGamesCount().Divide(gamesPlayed),
            MoreThanTwoGoalsGames = games.GetMoreThanTwoGoalScoredGamesCount().Divide(gamesPlayed),
            TwoToThreeGoalGames = games.GetTwoToThreeGoalScoredGamesCount().Divide(gamesPlayed),
            LessThanThreeGoalsAccuracy = games.Count(i => i.FTAG + i.FTHG < 3).Divide(gamesPlayed)
        };

        return teamData;
    }

    private static HeadToHead GetHeadToHeadDataBy(ICollection<HistoricalGame> games)
    {
        var gameCount = games.Count;

        var teamData = new HeadToHead
        {
            GamesPlayed = gameCount,
            HomeWin = games.Count(i => i.FTR == "H").Divide(gameCount),
            AwayWin = games.Count(i => i.FTR == "A").Divide(gameCount),
            Draw = games.Count(i => i.FTR == "D").Divide(gameCount),
            NoScored = games.GetNoGoalGameCount().Divide(gameCount),
            BothTeamScored = games.GetBothScoredGamesCount().Divide(gameCount),
            MoreThanTwoScored = games.GetMoreThanTwoGoalScoredGamesCount().Divide(gameCount),
            TwoToThreeScored = games.GetTwoToThreeGoalScoredGamesCount().Divide(gameCount),
            HalfTimeScored = games.Count(i => i.HTHG > 0 || i.HTAG > 0).Divide(gameCount),
            HomeSideScored = games.Count(i => i is { FTHG: > 0, FTAG: 0 }).Divide(gameCount),
            AwaySideScored = games.Count(i => i is { FTAG: > 0, FTHG: 0 }).Divide(gameCount),
            LessThanThreeGoal = games.Count(i => i.FTAG + i.FTHG < 3).Divide(gameCount)
        };

        return teamData;
    }

    private static int GetGoalSumBy(IReadOnlyCollection<HistoricalGame> games, string team, bool conceded = false) =>
        games.Where(i => i.HomeTeam == team).Sum(i => conceded ? i.FTAG : i.FTHG) +
        games.Where(i => i.AwayTeam == team).Sum(i => conceded ? i.FTHG : i.FTAG) ?? 0;



    public double GetHalftimeGoalGamesPercentageBy(List<HistoricalGame> games, string team)
    {
        var average = games.GetHalftimeGoalGamesCount().Divide(games.Count);

        return average;
    }

    public double GetHalftimeGoalAverageBy(List<HistoricalGame> games, string team)
    {
        var halftimeGoalsScored = games.GetHalftimeGoalScoredSumBy(team);
        var halftimeGoalsConceded = games.GetHalftimeGoalConcededSumBy(team);

        var average = (halftimeGoalsScored + halftimeGoalsConceded).Divide(games.Count);

        return average;
    }
    
    private static void TicketGenerated(List<Dictionary<string, double>> games)
    {
        var result = new Dictionary<string, double>();
        foreach (var game in games)
        {
            var orderedGames = game.OrderByDescending(i => i.Value).FirstOrDefault();
            result.Add(orderedGames.Key, orderedGames.Value);
        }

        var count = 0;
        foreach (var d in result.OrderByDescending(i => i.Value))
        {
            if (count == 0)
                Console.WriteLine("###### Generated Super Poison Ticket #######");

            if (count is 4 or 8 or 12)
                Console.WriteLine("###############################\n\n");

            if (count == 4)
                Console.WriteLine("######## Generated Second Schein ###########");

            if (count == 8)
                Console.WriteLine("######## Generated Third Schein ###########");

            Console.WriteLine(d.Key);

            count++;
        }

    }

    private static void GenerateTickets(Dictionary<string, double> gamesInOrder)
    {
        var count = 0;

        foreach (var game in gamesInOrder)
        {
            if (count == 0)
                Console.WriteLine("###### Generated Super Schein #######");

            if (count is 4 or 8 or 12)
                Console.WriteLine("###############################\n\n");


            if (count == 4)
                Console.WriteLine("######## Generated Second Schein ###########");


            if (count == 8)
                Console.WriteLine("######## Generated Third Schein ###########");



            count++;
        }
    }


    internal void Analyse(string homeTeam, string awayTeam, string league)
    {
        var service = new PoissonService(_historicalGames);
        var execute = service.Execute(homeTeam, awayTeam, league);

        // var cal = new CalculationService(service, _historicalGames, _upComingGames);
        //  cal.Execute(homeTeam, awayTeam, league);

        var list = PickTheBestProbability(homeTeam, awayTeam, execute);


    }

    private Dictionary<string, double> PickTheBestProbability(
        string homeTeam, string awayTeam,
        IList<PoissonProbability> analysePoisson, DateTime date = default, TimeSpan time = default)
    {
        date = date == default ? DateTime.Now.Date : date;
        time = time == default ? DateTime.Now.TimeOfDay : time;

        var oddProbabilitiesInDescOrder = analysePoisson
            .Where(ia => ia.Key is "AwayWin" or "HomeWin" or "Draw")
            .OrderByDescending(ii => ii.Probability)
            .ToList();

        var probabilitiesInDescOrder = analysePoisson
            .OrderByDescending(ii => ii.Probability)
            .Take(1)
            .ToList();

        var list = new Dictionary<string, double>();
        probabilitiesInDescOrder.ForEach(item =>
        {

            var match = $"{homeTeam}:{awayTeam}";
            if (DerbyTeams.GetDerbyMatches().Contains(match))
                match = $"DERBY MATCH!! {match}";

            switch (item.Key)
            {
                case nameof(HistoricalGame.HomeWin):
                    if (item.Probability >= 0.70)
                    {
                        var msg = $"{date:d} {time:g}: {match} Home win = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.AwayWin):
                    if (item.Probability >= 0.70)
                    {

                        var msg = $"{date:d} {time:g}: {match} Away win = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.Draw):
                    if (item.Probability >= 0.68)
                    {
                        var msg = $"{date:d} {time:g}: {match} Draw =  {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.BothTeamScore):
                    if (item.Probability > 0.64)
                    {
                        var msg = $"{date:d} {time:g}: {match} Both score = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.MoreThanTwoGoals):
                    if (item.Probability > 0.60)
                    {
                        var msg = $"{date:d} {time:g}: {match} More Than two goals = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.TwoToThree):
                    if (item.Probability > 0.60)
                    {
                        var msg = $"{date:d} {time:g}: {match} Two to three goals = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.LessThanTwoGoals):
                    if (item.Probability > 0.70)
                    {
                        var msg = $"{date:d} {time:g}: {match} less than three goals = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
            }
        });

        oddProbabilitiesInDescOrder.ForEach(item =>
        {
            var match = $"{homeTeam}:{awayTeam}";
            if (DerbyTeams.GetDerbyMatches().Contains(match))
                match = $"DERBY MATCH!! {match}";

            switch (item.Key)
            {
                case nameof(HistoricalGame.HomeWin):
                    if (item.Probability > 0.70)
                    {
                        var msg = $"{date:d} {time:g}: {match} Home win = {Math.Round(item.Probability, 2)}%";
                        //list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.AwayWin):
                    if (item.Probability >= 0.70)
                    {

                        var msg = $"{date:d} {time:g}: {match} Away win = {Math.Round(item.Probability, 2)}%";
                        //list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.Draw):
                    if (item.Probability >= 0.68)
                    {
                        var msg = $"{date:d} {time:g}: {match} Draw =  {Math.Round(item.Probability, 2)}%";
                        // list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
            }

        });

        return list;
    }
}
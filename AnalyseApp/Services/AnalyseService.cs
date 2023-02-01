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
    private List<GameData> _historicalGames = new();
    private List<GameData> _upComingGames = new();
    
    // Rajev
    //private const double LastSixGamesWeight = 0.40;
    //private const double HistoricalGamesWeight = 0.20;
    //private const double HeadToHeadGamesWeight = 0.25;
    //private const double PoisonProbabilityWeight = 0.15;
    // WK/SM
    //private const double LastSixGamesWeight = 0.40;
    //private const double HistoricalGamesWeight = 0.10;
    //private const double HeadToHeadGamesWeight = 0.25;
    //private const double PoisonProbabilityWeight = 0.25;
    // Shivm
    private const double LastSixGamesWeight = 0.30;
    private const double HistoricalGamesWeight = 0.10;
    private const double HeadToHeadGamesWeight = 0.30;
    private const double PoisonProbabilityWeight = 0.30;
    private List<GameProbability> Probabilities = new();

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

            var currentFileGames = csv.GetRecords<GameData>();
            _historicalGames.AddRange(currentFileGames);
        }

        _historicalGames = _historicalGames
            .OrderByDescending(i => DateTime.Parse(i.Date)).ToList();
        return this;
    }
    
    internal AnalyseService CreateMlFile(string league)
    {
        var records = _historicalGames
            .Where(i => i.Div == league)
            .Select(s => new MatchData
            {
                HomeTeam = s.HomeTeam,
                AwayTeam = s.AwayTeam,
                HomeTeamGoals = Convert.ToSingle(s.FTHG),
                AwayTeamGoals = Convert.ToSingle(s.FTAG),
                HomeTeamHalfTimeGoals = Convert.ToSingle(s.HTHG),
                AwayTeamHalfTimeGoals = Convert.ToSingle(s.HTAG),
                AwayTeamShots = Convert.ToSingle(s.AS),
                HomeTeamShots = Convert.ToSingle(s.HS)
            })
            .ToList();
        using (var writer = new StreamWriter($"{FileDir}\\ml\\{league}.csv"))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteHeader<MatchData>();
            csv.NextRecord();
            foreach (var record in records)
            {
                csv.WriteRecord(record);
                csv.NextRecord();
            }
        }
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

            var currentFileGames = csv.GetRecords<GameData>();

            _upComingGames.AddRange(currentFileGames);
        }

        _upComingGames = _upComingGames.OrderByDescending(i => i.Date).ToList();
        
        return this;
    }

    internal void AnalyseGames()
    {
        var poissonService = new PoissonService(_historicalGames);
        
        foreach (var comingGame in _upComingGames)
        {
            if (DerbyTeams.PopularTeams().Contains(comingGame.HomeTeam) || DerbyTeams.PopularTeams().Contains(comingGame.AwayTeam))
                continue;

            var gameProbability = new GameProbability
            {
                Title = $"{comingGame.HomeTeam}:{comingGame.AwayTeam}",
                Date = DateTime.Parse(comingGame.Date).Date,
                Time = DateTime.Parse(comingGame.Date)
            };
            var lastSixGames = LastSixGames(comingGame.HomeTeam, comingGame.AwayTeam);
            var lastSixSeason = LastSixSeason(comingGame.HomeTeam, comingGame.AwayTeam);
            var probability = poissonService.Execute(comingGame.HomeTeam, comingGame.AwayTeam, comingGame.Div);

            if (comingGame.HomeTeam == "Dortmund" || comingGame.HomeTeam == "Cremonese" || comingGame.HomeTeam == "Nice")
            {
                
            }
            NoGoalAverage(
                probability.FirstOrDefault(i => i.Key == "ZeroZeroGoals")?.Probability ?? 0,
                lastSixGames, lastSixSeason,
                gameProbability);
            
            HalfTimeScoredGames(
                lastSixGames, lastSixSeason,
                gameProbability);

            BothScoreGames(
                probability.FirstOrDefault(i => i.Key == "BothTeamScore")?.Probability ?? 0,
                lastSixGames, lastSixSeason,
                gameProbability);
            
            MoreThanTwoGoalsGames(
                probability.FirstOrDefault(i => i.Key == "MoreThanTwoGoals")?.Probability ?? 0,
                lastSixGames, lastSixSeason,
                gameProbability);
            
            TwoToThreeGoalsGames(
                probability.FirstOrDefault(i => i.Key == "TwoToThree")?.Probability ?? 0,
                lastSixGames, lastSixSeason,
                gameProbability);
            
            OneSideGoalsGames(
                probability.FirstOrDefault(i => i.Key == "OneSideGoal")?.Probability ?? 0,
                lastSixGames, lastSixSeason,
                gameProbability);
            
            WinGames(
                probability.FirstOrDefault(i => i.Key == "HomeWin")?.Probability ?? 0,
                probability.FirstOrDefault(i => i.Key == "AwayWin")?.Probability ?? 0,
                lastSixGames, lastSixSeason,
                gameProbability
                ); 
            
            if (gameProbability is { Qualified: true, Probability: > 0.62 })
            //if (gameProbability is { Qualified: true })
                Probabilities.Add(gameProbability);
        }

        FilterBestGames();
    }

    private void FilterBestGames()
    {
        var orderedProbabilities = Probabilities
            .OrderByDescending(ii => ii.Probability)
            .ToList();

        for (var i = 0; i < orderedProbabilities.Count; i++)
        {
            var item = orderedProbabilities[i];
            var message = "";
            var isDerby = IsDerbyMatch(item.Title);

            if (isDerby)
            {
                message = "DERBY MATCH!!";
            }

            message += $"{item.Date:d} {item.Time:g}: {item.Title} {item.ProbabilityKey} = {Math.Round(item.Probability ?? 0, 2)}%";

            switch (i)
            {
                case 0:
                    Console.WriteLine("###### Generated Super Poison Ticket #######");
                    break;
                case 4:
                    Console.WriteLine("###############################\n\n");
                    Console.WriteLine("######## Generated Second Schein ###########");
                    break;
                case 8:
                    Console.WriteLine("###############################\n\n");
                    if (i != orderedProbabilities.Count)
                    {
                        Console.WriteLine("######## Generated Third Schein ###########");
                    }
                    break;
            }

            if (!isDerby)
            {
                Console.WriteLine(message);
            }
            
            if (i == orderedProbabilities.Count)
            {
                Console.WriteLine("###############################");
            }
        }
    }

    private static bool IsDerbyMatch(string title)
    {
        return DerbyTeams.GetDerbyMatches().Contains(title);
    }


    private  NextGame LastSixGames(string homeTeam, string awayTeam)
    {
        var currentMatches = _historicalGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == homeTeam ||
                        i.HomeTeam == homeTeam || i.AwayTeam == awayTeam)
            .GetGameDataBy(2022, 2023);
        
        var lastSixHomeHomeMatches = currentMatches
            .Where(i => i.HomeTeam == homeTeam)
            .Take(4)
            .ToList();

        var lastSixHomeAwayMatches = currentMatches
            .Where(i => i.AwayTeam == homeTeam)
            .Take(4)
            .ToList();
        
        var lastSixAwayHomeMatches = currentMatches
            .Where(i => i.HomeTeam == awayTeam)
            .Take(6)
            .ToList();
        
        var lastSixAwayAwayMatches = currentMatches
            .Where(i => i.AwayTeam == awayTeam)
            .Take(6)
            .ToList();
        
        var homeHomeTeamData = GetTeamDataBy(lastSixHomeHomeMatches, homeTeam);
        var homeAwayTeamData = GetTeamDataBy(lastSixHomeAwayMatches, homeTeam);
        var awayHomeTeamData = GetTeamDataBy(lastSixAwayHomeMatches, awayTeam);
        var awayAwayTeamData = GetTeamDataBy(lastSixAwayAwayMatches, awayTeam);

        var result = new NextGame
        {
            HomeHome = homeHomeTeamData,
            HomeAway = awayHomeTeamData,
            AwayAway = awayAwayTeamData,
            AwayHome = homeAwayTeamData
        };

        return result;
        
    }
    
    private NextGame LastSixSeason(string homeTeam, string awayTeam)
    {
        var lastSixHomeGames = _historicalGames
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();

        var lastSixAwayGames = _historicalGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();
        
        var headToHeadGames = _historicalGames
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam || 
                                i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .ToList();

        
        var homeTeamData = GetTeamDataBy(lastSixHomeGames, homeTeam);
        var awayTeamData = GetTeamDataBy(lastSixAwayGames, awayTeam);
        var headToHead = GetHeadToHeadDataBy(headToHeadGames);
        
        var result = new NextGame
        {
            HomeHome = homeTeamData,
            AwayAway = awayTeamData,
            HeadToHead = headToHead
            
        };

        return result;
    }
    
    private static Team GetTeamDataBy(IList<GameData> games, string team)
    {
        var gamesPlayed = games.Count;
        
        var teamData = new Team
        {
            GamesPlayed = games.Count,
            Win = games.GetWinGamesCountBy(team).Divide(gamesPlayed),
            Loss = games.GetLossGamesCountBy(team).Divide(gamesPlayed),
            Draw = games.Count(i => i.FTR == "D").Divide(gamesPlayed),
            NoGoalGames = games.GetNoGoalGameCount().Divide(gamesPlayed),
            GoalsScored = games.GetGoalScoredSumBy(team).Divide(gamesPlayed),
            GoalsConceded = games.GetGoalConcededSumBy(team).Divide(gamesPlayed),
            OneSideGoalGames = games.GetOneSideGoalGamesCount().Divide(gamesPlayed),
            HalftimeGoalsScored = games.GetHalftimeGoalScoredSumBy(team).Divide(gamesPlayed),
            HalftimeGoalsConceded = games.GetHalftimeGoalConcededSumBy(team).Divide(gamesPlayed),
            HalftimeGoalGames = games.GetHalftimeGoalScoredGamesCount().Divide(gamesPlayed),
            WinOneSideGoalGames = games.Count(i => i.HomeTeam == team && i is { FTHG: > 0, FTAG: 0 } ||
                                                   i.AwayTeam == team && i is { FTHG: 0, FTAG: > 0 })
                                        .Divide(gamesPlayed),
            BothScoreGames = games.GetBothScoredGamesCount().Divide(gamesPlayed),
            MoreThanTwoGoalsGames = games.GetMoreThanTwoGoalScoredGamesCount().Divide(gamesPlayed),
            TwoToThreeGoalGames = games.GetTwoToThreeGoalScoredGamesCount().Divide(gamesPlayed)
        };

        return teamData;
    }
    
    private static HeadToHead GetHeadToHeadDataBy(ICollection<GameData> games)
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
            HomeSideScored = games.Count(i => i.FTHG is > 0 and < 3 && i.FTAG == 0).Divide(gameCount),
            AwaySideScored = games.Count(i => i.FTAG is > 0 and < 3 && i.FTHG == 0).Divide(gameCount)
        };

        return teamData;
    }
    
    
    private static void NoGoalAverage(double probability, NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        var homeAverage = (lastSixGames.HomeHome.NoGoalGames * 0.5 + lastSixGames.HomeAway.NoGoalGames * 0.5) * LastSixGamesWeight  +
                          allGames.HomeHome.NoGoalGames * HistoricalGamesWeight +
                          allGames.HeadToHead.NoScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;
        
        var awayAverage = (lastSixGames.AwayHome.NoGoalGames * 0.5 + lastSixGames.AwayAway.NoGoalGames * 0.5) * LastSixGamesWeight  +
                          allGames.AwayAway.NoGoalGames * HistoricalGamesWeight +
                          allGames.HeadToHead.NoScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;

        gameProbability.NoGoalAverage = homeAverage * 0.50 + awayAverage * 0.50;

        if (gameProbability.NoGoalAverage > 0.60)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability =  gameProbability.NoGoalAverage;
            gameProbability.ProbabilityKey = nameof(NoGoalAverage);
        }
    }
    
    private static void BothScoreGames(double probability, NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        var homeAverage = (lastSixGames.HomeHome.BothScoreGames * 0.5 + lastSixGames.HomeAway.BothScoreGames * 0.5) * LastSixGamesWeight  +
                          allGames.HomeHome.BothScoreGames * HistoricalGamesWeight +
                          allGames.HeadToHead.BothTeamScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;
        
        var awayAverage = (lastSixGames.AwayHome.BothScoreGames * 0.5 + lastSixGames.AwayAway.BothScoreGames * 0.5) * LastSixGamesWeight  +
                          allGames.AwayAway.BothScoreGames * HistoricalGamesWeight +
                          allGames.HeadToHead.BothTeamScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;

        // Home and Away both are able at least 68% to score a goal and the previous probability is not bigger than current
        // than this would be qualified.
        if (homeAverage > 0.68 && awayAverage > 0.68 && gameProbability.Probability is null ||
            gameProbability.Probability < probability || gameProbability.ProbabilityKey == nameof(HalfTimeScoredGames))
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = homeAverage * 0.50 + awayAverage * 0.50;
            gameProbability.ProbabilityKey = nameof(BothScoreGames);
        }

        if (gameProbability.Probability is null)
        {
            gameProbability.Probability = (homeAverage * 0.50 + awayAverage * 0.50);
            gameProbability.ProbabilityKey = nameof(BothScoreGames);
        }
    }
    
    private static void MoreThanTwoGoalsGames(double probability, NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        var homeAverage = (lastSixGames.HomeHome.MoreThanTwoGoalsGames * 0.5 + lastSixGames.HomeAway.MoreThanTwoGoalsGames * 0.5) * LastSixGamesWeight  +
                          allGames.HomeHome.MoreThanTwoGoalsGames * HistoricalGamesWeight +
                          allGames.HeadToHead.MoreThanTwoScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;
        
        var awayAverage = (lastSixGames.AwayHome.MoreThanTwoGoalsGames * 0.5 + lastSixGames.AwayAway.MoreThanTwoGoalsGames * 0.5) * LastSixGamesWeight  +
                          allGames.AwayAway.MoreThanTwoGoalsGames * HistoricalGamesWeight +
                          allGames.HeadToHead.MoreThanTwoScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;

        var finalAverage = homeAverage * 0.50 + awayAverage * 0.50;
        // Home and Away both are able at least 68% to score a goal and the previous probability is not bigger than current
        // than this would be qualified.
        if (homeAverage > 0.68 && awayAverage > 0.68 && gameProbability.Probability < finalAverage || gameProbability.ProbabilityKey == nameof(HalfTimeScoredGames))
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = finalAverage;
            gameProbability.ProbabilityKey = nameof(MoreThanTwoGoalsGames);
        }
    }
    
    private static void TwoToThreeGoalsGames(double probability, NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        
        var homeAverage = (lastSixGames.HomeHome.TwoToThreeGoalGames * 0.5 + lastSixGames.HomeAway.TwoToThreeGoalGames * 0.5) * LastSixGamesWeight  +
                          allGames.HomeHome.TwoToThreeGoalGames * HistoricalGamesWeight +
                          allGames.HeadToHead.TwoToThreeScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;
        
        var awayAverage = (lastSixGames.AwayHome.TwoToThreeGoalGames * 0.5 + lastSixGames.AwayAway.TwoToThreeGoalGames * 0.5) * LastSixGamesWeight  +
                          allGames.AwayAway.TwoToThreeGoalGames * HistoricalGamesWeight +
                          allGames.HeadToHead.TwoToThreeScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;

        var finalAverage = homeAverage * 0.50 + awayAverage * 0.50;
        // Home and Away both are able at least 68% to score more than two goals and the previous probability is not bigger than current
        // than this would be qualified.
        if (homeAverage > 0.68 && awayAverage > 0.68 && gameProbability.Probability < finalAverage || gameProbability.ProbabilityKey == nameof(HalfTimeScoredGames))
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = finalAverage;
            gameProbability.ProbabilityKey = nameof(TwoToThreeGoalsGames);
        }
    }
    
    private static void OneSideGoalsGames(double probability, NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        var homeAverage = (lastSixGames.HomeHome.OneSideGoalGames * 0.5 + lastSixGames.HomeAway.OneSideGoalGames * 0.5)  * LastSixGamesWeight  +
                          allGames.HomeHome.OneSideGoalGames * HistoricalGamesWeight +
                          allGames.HeadToHead.HomeSideScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;
        
        var awayAverage = (lastSixGames.AwayHome.OneSideGoalGames * 0.5 + lastSixGames.AwayHome.OneSideGoalGames * 0.5)  * LastSixGamesWeight  +
                          allGames.AwayAway.OneSideGoalGames * HistoricalGamesWeight +
                          allGames.HeadToHead.AwaySideScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;

        var finalAverage = homeAverage * 0.50 + awayAverage * 0.50;
        // Home and Away both are able score two to three goals and the previous probability is not bigger than current
        // than this would be qualified.
        if (homeAverage > 0.68 && awayAverage > 0.68 && gameProbability.Probability < finalAverage || gameProbability.ProbabilityKey == nameof(HalfTimeScoredGames))
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = finalAverage;
            gameProbability.ProbabilityKey = nameof(OneSideGoalsGames);
        }
    }
    
    private static void WinGames(double homeWinProbability, double awayProbability, NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        var homeWinAverage = (lastSixGames.HomeHome.Win * 0.50 + lastSixGames.HomeAway.Win * 0.50) * LastSixGamesWeight +
                             allGames.HomeHome.Win * HistoricalGamesWeight +
                             allGames.HeadToHead.HomeWin * HeadToHeadGamesWeight +
                             homeWinProbability * PoisonProbabilityWeight;
        
        var awayWinAverage = (lastSixGames.AwayHome.Win * 0.50 + lastSixGames.AwayAway.Win * 0.50) * LastSixGamesWeight +
                             allGames.AwayAway.Win * HistoricalGamesWeight +
                             allGames.HeadToHead.AwayWin * (HeadToHeadGamesWeight + 0.10) +
                             awayProbability * PoisonProbabilityWeight;

        var homeOneGoalAverage = (lastSixGames.HomeHome.BothScoreGames * 0.50 + lastSixGames.HomeAway.BothScoreGames * 0.50) * (LastSixGamesWeight + 0.15)  +
                          allGames.HomeHome.BothScoreGames * HistoricalGamesWeight +
                          allGames.HeadToHead.BothTeamScored * HeadToHeadGamesWeight;
        
        var awayOneGoalAverage = (lastSixGames.AwayHome.BothScoreGames * 0.50 + lastSixGames.AwayAway.BothScoreGames * 0.50) * (LastSixGamesWeight + 0.15)  +
                                 allGames.AwayAway.BothScoreGames * HistoricalGamesWeight +
                                 allGames.HeadToHead.BothTeamScored * (HeadToHeadGamesWeight + 0.10);

        var homeHalftimeAverage = (lastSixGames.HomeHome.HalftimeGoalGames * 0.50 + lastSixGames.AwayHome.HalftimeGoalGames * 0.50) * (LastSixGamesWeight + 0.15)  +
                                  allGames.HomeHome.HalftimeGoalGames * (HistoricalGamesWeight + 0.10) +
                                  allGames.HeadToHead.HalfTimeScored * HeadToHeadGamesWeight;
        
        var awayHalftimeAverage = (lastSixGames.HomeAway.HalftimeGoalGames * 0.50 + lastSixGames.AwayAway.HalftimeGoalGames * 0.50) * (LastSixGamesWeight + 0.15)  +
                                  allGames.AwayAway.HalftimeGoalGames * (HistoricalGamesWeight + 0.10) +
                                  allGames.HeadToHead.HalfTimeScored * HeadToHeadGamesWeight;
        
        var testHomeAverage = homeWinAverage * 0.40 + homeOneGoalAverage * 0.30 + homeHalftimeAverage * 0.30;
        var testAwayAverage = awayWinAverage * 0.40 + awayOneGoalAverage * 0.30 + awayHalftimeAverage * 0.30;
        var difference = testHomeAverage > testAwayAverage
            ? testHomeAverage - testAwayAverage
            : testAwayAverage - testHomeAverage;
        
        if (homeWinAverage > awayWinAverage && homeOneGoalAverage > awayOneGoalAverage &&
            gameProbability.Probability < 0.68 || gameProbability.ProbabilityKey == nameof(HalfTimeScoredGames))
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = testHomeAverage;
            gameProbability.ProbabilityKey = "HomeWin";
        }
        if (homeWinAverage < awayWinAverage && homeOneGoalAverage < awayOneGoalAverage &&
            gameProbability.Probability < 0.68 || gameProbability.ProbabilityKey == nameof(HalfTimeScoredGames))
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = testAwayAverage;
            gameProbability.ProbabilityKey = "AwayWin";
        }
        /*
        if (homeWinAverage > awayWinAverage && homeOneGoalAverage > awayOneGoalAverage &&
            homeOneGoalAverage - awayOneGoalAverage > 0.25 && difference > 0.25 && gameProbability.Probability < 0.68 || gameProbability.ProbabilityKey == nameof(HalfTimeScoredGames))
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = testHomeAverage;
            gameProbability.ProbabilityKey = "HomeWin";
        }
        if (homeWinAverage < awayWinAverage && homeOneGoalAverage < awayOneGoalAverage &&
            awayOneGoalAverage - homeOneGoalAverage > 0.25 && difference > 0.25 && gameProbability.Probability < 0.68 || gameProbability.ProbabilityKey == nameof(HalfTimeScoredGames))
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = testAwayAverage;
            gameProbability.ProbabilityKey = "AwayWin";
        }*/
    }

    // Has individual weighting because the probability implemented yet.
    private static void HalfTimeScoredGames(NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        var homeAverage = (lastSixGames.HomeHome.HalftimeGoalGames * 0.50 + lastSixGames.AwayHome.HalftimeGoalGames * 0.50) * (LastSixGamesWeight + 0.15)  +
                          allGames.HomeHome.HalftimeGoalGames * (HistoricalGamesWeight + 0.10) +
                          allGames.HeadToHead.HalfTimeScored * HeadToHeadGamesWeight;
        
        var awayAverage = (lastSixGames.AwayHome.HalftimeGoalGames * 0.50 + lastSixGames.AwayAway.HalftimeGoalGames * 0.50) * (LastSixGamesWeight + 0.15)  +
                          allGames.AwayAway.HalftimeGoalGames * (HistoricalGamesWeight + 0.10) +
                          allGames.HeadToHead.HalfTimeScored * HeadToHeadGamesWeight;

        gameProbability.HalftimeGoalAverage = homeAverage * 0.50 + awayAverage * 0.50;
        // Home and Away both are able at least 68% to score a goal in halftime and the previous probability is not bigger than 68%
        // than this would be qualified.
        if (homeAverage > 0.68 && awayAverage > 0.68 && gameProbability.Probability is null)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = gameProbability.HalftimeGoalAverage;
            gameProbability.ProbabilityKey = nameof(HalfTimeScoredGames);
        }
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
                case nameof(GameData.HomeWin):
                    if (item.Probability >= 0.70)
                    {
                        var msg = $"{date:d} {time:g}: {match} Home win = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(GameData.AwayWin): 
                    if (item.Probability >= 0.70)
                    {
                        
                        var msg = $"{date:d} {time:g}: {match} Away win = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(GameData.Draw): 
                    if (item.Probability >= 0.68)
                    {
                        var msg = $"{date:d} {time:g}: {match} Draw =  {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(GameData.BothTeamScore):
                    if (item.Probability > 0.64)
                    {
                        var msg = $"{date:d} {time:g}: {match} Both score = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(GameData.MoreThanTwoGoals):
                    if (item.Probability > 0.60)
                    {
                        var msg = $"{date:d} {time:g}: {match} More Than two goals = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(GameData.TwoToThree):
                    if (item.Probability > 0.60)
                    { 
                        var msg = $"{date:d} {time:g}: {match} Two to three goals = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(GameData.LessThanTwoGoals):
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
                case nameof(GameData.HomeWin):
                    if (item.Probability > 0.70)
                    {
                        var msg = $"{date:d} {time:g}: {match} Home win = {Math.Round(item.Probability, 2)}%";
                        //list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(GameData.AwayWin): 
                    if (item.Probability >= 0.70)
                    {
                        
                        var msg = $"{date:d} {time:g}: {match} Away win = {Math.Round(item.Probability, 2)}%";
                        //list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(GameData.Draw): 
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
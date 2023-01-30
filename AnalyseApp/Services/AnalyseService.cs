using System.Globalization;
using AnalyseApp.Commons.Constants;
using AnalyseApp.Extensions;
using AnalyseApp.Handlers;
using AnalyseApp.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Tensorflow.Operations.Initializers;

namespace AnalyseApp.Services;

public class AnalyseService
{
    private const string FileDir = "C:\\shivm\\AnalyseApp\\data";
    private List<GameData> _historicalGames = new();
    private List<GameData> _upComingGames = new();
    private IPoissonService _poissonService = new PoissonService(null);
    
    protected const double LastSixGamesWeight = 0.40;
    protected const double HistoricalGamesWeight = 0.10;
    protected const double HeadToHeadGamesWeight = 0.25;
    protected const double PoisonProbabilityWeight = 0.25;

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
        _poissonService = new PoissonService(_historicalGames);
        var teamData = new TeamData();
        foreach (var comingGame in _upComingGames)
        {
            var lastSixGames = LastSixGames(comingGame.HomeTeam, comingGame.AwayTeam);
            var lastSixSeason = LastSixSeason(comingGame.HomeTeam, comingGame.AwayTeam);
            var probability = _poissonService.Execute(comingGame.HomeTeam, comingGame.AwayTeam, comingGame.Div);

            teamData.NoGoalGames = NoGoalAverage(
                probability.FirstOrDefault(i => i.Key == "ZeroZeroGoals")?.Probability ?? 0,
                lastSixGames, lastSixSeason);

            teamData.BothScoreGames = BothScoreGames(
                probability.FirstOrDefault(i => i.Key == "BothTeamScore")?.Probability ?? 0,
                lastSixGames, lastSixSeason);
            
            teamData.MoreThanTwoGoalsGames = MoreThanTwoGoalsGames(
                probability.FirstOrDefault(i => i.Key == "MoreThanTwoGoals")?.Probability ?? 0,
                lastSixGames, lastSixSeason);
            
            teamData.TwoToThreeGoalGames = TwoToThreeGoalsGames(
                probability.FirstOrDefault(i => i.Key == "TwoToThree")?.Probability ?? 0,
                lastSixGames, lastSixSeason);
            
            teamData.OneSideGoalGames = OneSideGoalsGames(
                probability.FirstOrDefault(i => i.Key == "TwoToThree")?.Probability ?? 0,
                lastSixGames, lastSixSeason);
            
            teamData.Win = WinGames(
                probability.FirstOrDefault(i => i.Key == "HomeWin")?.Probability ?? 0,
                probability.FirstOrDefault(i => i.Key == "AwayWin")?.Probability ?? 0,
                lastSixGames, lastSixSeason);
            
        }
        
        
         var games = new List<Dictionary<string, double>>();
        foreach (var upComingGame in _upComingGames)
        {
           // dec.Train(_historicalGames, upComingGame.HomeTeam, upComingGame.AwayTeam, upComingGame.Div);
           // dec.Predict(new[] { "FTR" });
            var analysePoisson = _poissonService.Execute(
                upComingGame.HomeTeam,
                upComingGame.AwayTeam,
                upComingGame.Div
            );

            var game = PickTheBestProbability(upComingGame.HomeTeam,
                upComingGame.AwayTeam, analysePoisson,
                Convert.ToDateTime(upComingGame.Date), 
                Convert.ToDateTime(upComingGame.Time).TimeOfDay);

            if(game.Count != 0)
                games.Add(game);
        }

        TicketGenerated(games);
    }

    
    
    
    private  NextGame LastSixGames(string homeTeam, string awayTeam)
    {
        var currentMatches = _historicalGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == homeTeam ||
                        i.HomeTeam == homeTeam || i.AwayTeam == awayTeam)
            .GetGameDataBy(2022, 2023);
        
        var lastSixHomeMatches = currentMatches
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .Take(6)
            .ToList();

        var lastSixAwayMatches = currentMatches
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .Take(6)
            .ToList();
        
        var homeTeamData = GetTeamDataBy(lastSixHomeMatches, homeTeam);
        var awayTeamData = GetTeamDataBy(lastSixAwayMatches, awayTeam);

        var result = new NextGame
        {
            Home = homeTeamData,
            Away = awayTeamData
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
            Home = homeTeamData,
            Away = awayTeamData,
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
    
    
    private static Average NoGoalAverage(double probability, NextGame lastSixGames, NextGame allGames)
    {
        var lastSix = lastSixGames.Home.NoGoalGames * LastSixGamesWeight +
                      lastSixGames.Away.NoGoalGames * LastSixGamesWeight;
        
        var allGamesNoScore = allGames.Home.NoGoalGames * HistoricalGamesWeight +
                              allGames.Away.NoGoalGames * HistoricalGamesWeight;

        var average = (lastSix + allGamesNoScore) * 0.50 + 
                      allGames.HeadToHead.NoScored * HeadToHeadGamesWeight + 
                      probability * PoisonProbabilityWeight;
        
        return new Average(average, average < 0.20);
    }
    
    private static Average BothScoreGames(double probability, NextGame lastSixGames, NextGame allGames)
    {
        var lastSixBothScored = lastSixGames.Home.BothScoreGames * LastSixGamesWeight +
                                lastSixGames.Away.BothScoreGames  * LastSixGamesWeight;
        
        var allGamesBothScored = allGames.Home.BothScoreGames * HistoricalGamesWeight +
                                 allGames.Away.BothScoreGames  * HistoricalGamesWeight;

        var average = (lastSixBothScored + allGamesBothScored) * 0.50 + 
                      allGames.HeadToHead.BothTeamScored * HeadToHeadGamesWeight + 
                      probability * PoisonProbabilityWeight;

        return new Average(average, average >= 0.68);
    }
    
    private static Average MoreThanTwoGoalsGames(double probability, NextGame lastSixGames, NextGame allGames)
    {
        var lastSixBothScored = lastSixGames.Home.MoreThanTwoGoalsGames * LastSixGamesWeight +
                                lastSixGames.Away.MoreThanTwoGoalsGames  * LastSixGamesWeight;
        
        var allGamesBothScored = allGames.Home.MoreThanTwoGoalsGames * HistoricalGamesWeight +
                                 allGames.Away.MoreThanTwoGoalsGames  * HistoricalGamesWeight;

        var average = (lastSixBothScored + allGamesBothScored) * 0.50 + 
                      allGames.HeadToHead.MoreThanTwoScored * HeadToHeadGamesWeight + 
                      probability * PoisonProbabilityWeight;

        return new Average(average, average >= 0.68);
    }
    
    private static Average TwoToThreeGoalsGames(double probability, NextGame lastSixGames, NextGame allGames)
    {
        var lastSixBothScored = lastSixGames.Home.TwoToThreeGoalGames * LastSixGamesWeight +
                                lastSixGames.Away.TwoToThreeGoalGames  * LastSixGamesWeight;
        
        var allGamesBothScored = allGames.Home.TwoToThreeGoalGames * HistoricalGamesWeight +
                                 allGames.Away.TwoToThreeGoalGames  * HistoricalGamesWeight;

        var average = (lastSixBothScored + allGamesBothScored) * 0.50 + 
                      allGames.HeadToHead.TwoToThreeScored * HeadToHeadGamesWeight + 
                      probability * PoisonProbabilityWeight;

        return new Average(average, average >= 0.68);
    }
    
    private static Average OneSideGoalsGames(double probability, NextGame lastSixGames, NextGame allGames)
    {
        var lastSixBothScored = lastSixGames.Home.OneSideGoalGames * LastSixGamesWeight +
                                lastSixGames.Away.OneSideGoalGames  * LastSixGamesWeight;
        
        var allGamesBothScored = allGames.Home.OneSideGoalGames * HistoricalGamesWeight +
                                 allGames.Away.OneSideGoalGames  * HistoricalGamesWeight;

        var average = (lastSixBothScored + allGamesBothScored) * 0.50 + 
                    (allGames.HeadToHead.AwaySideScored + allGames.HeadToHead.HomeSideScored) * HeadToHeadGamesWeight + 
                    probability * PoisonProbabilityWeight;

        return new Average(average, average >= 0.68);
    }
    
    private static Average WinGames(double homeWinProbability, double awayProbability, NextGame lastSixGames, NextGame allGames)
    {
        var homeWinAverage = lastSixGames.Home.Win * LastSixGamesWeight +
                                allGames.Home.Win * HistoricalGamesWeight +
                                allGames.HeadToHead.HomeWin * HeadToHeadGamesWeight +
                                homeWinProbability * PoisonProbabilityWeight;
        
        var awayWinAverage = lastSixGames.Away.Win * LastSixGamesWeight +
                             allGames.Away.Win * HistoricalGamesWeight +
                             allGames.HeadToHead.AwayWin * HeadToHeadGamesWeight +
                             awayProbability * PoisonProbabilityWeight;

        if (homeWinAverage > 0.68 && homeWinAverage > awayWinAverage)
            return new Average(homeWinAverage, homeWinAverage >= 0.68, $"Home Win this Game with {homeWinAverage}%");
        
        if (awayWinAverage > 0.68 && awayWinAverage > homeWinAverage)
            return new Average(awayWinAverage, awayWinAverage >= 0.68, $"Away Win this Game with {awayWinAverage}%");

        return new Average(awayWinAverage + homeWinAverage, false, "No direct predictions");
    }

    private static Average HalfTimeScoredGames(double probability, NextGame lastSixGames, NextGame allGames)
    {
        var lastSixBothScored = lastSixGames.Home.HalftimeGoalGames * LastSixGamesWeight +
                                lastSixGames.Away.HalftimeGoalGames  * LastSixGamesWeight;
        
        var allGamesBothScored = allGames.Home.HalftimeGoalGames * HistoricalGamesWeight +
                                 allGames.Away.HalftimeGoalGames  * HistoricalGamesWeight;

        var average = (lastSixBothScored + allGamesBothScored) * 0.50 + 
                      allGames.HeadToHead.HalfTimeScored * HeadToHeadGamesWeight;

        return new Average(average, average >= 0.68);
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


       if (league == "D1")
       {
           var test = PossibleProbabilities(homeTeam, awayTeam);
       }
    }

    private Dictionary<string, double> PossibleProbabilities(string homeTeam, string awayTeam)
    {
        var output = new Dictionary<string, double>();
        for (var homeScore = 0; homeScore <= 10; homeScore++)
        {
            for (var awayScore = 0; awayScore <= 10; awayScore++)
            {
                var probability = BundesligaAnalysis(
                    homeTeam,
                    awayTeam,
                    homeScore,
                    awayScore
                );
                output.Add($"{homeTeam}:{awayTeam}{homeScore}-{awayScore}", probability);
            }
        }
        return output;
    }

    private static float BundesligaAnalysis(string homeTeam, string awayTeam, int homeGoal, float awayGoal)
    {
        //Load sample data
        var sampleData = new Bundesliga.ModelInput
        {
            HomeTeam = @$"{homeTeam}",
            AwayTeam = @$"{awayTeam}",
            AwayTeamGoals = awayGoal,
            HomeTeamGoals= homeGoal,
            HomeTeamFullTimeGoals = homeGoal,
            AwayTeamFullTimeGoals = awayGoal,
            PredictedLabel = 0F,
        };

        //Load model and predict output
        var result = Bundesliga.Predict(sampleData);

        var score= result.Score;
        return score;
    }

    
    private Dictionary<string, double> PickTheBestProbability(
        string homeTeam, string awayTeam, 
        IEnumerable<PoissonProbability> analysePoisson, DateTime date = default, TimeSpan time = default)
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
                case nameof(GameData.AwayWin) or nameof(GameData.Draw) or nameof(GameData.HomeWin):
                    break;
                case nameof(GameData.BothTeamScore):
                    if (item.Probability > 0.62)
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
                    if (item.Probability > 0.68)
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
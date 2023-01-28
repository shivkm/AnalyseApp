using System.Drawing;
using System.Globalization;
using AnalyseApp.Commons.Constants;
using AnalyseApp.Extensions;
using AnalyseApp.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class AnalyseService
{
    private const string FileDir = "C:\\shivm\\AnalyseApp\\data";
    private List<GameData> _historicalGames = new();
    private List<GameData> _upComingGames = new();
    private IPoissonService _poissonService = new PoissonService(null);

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

        _historicalGames = _historicalGames.OrderByDescending(i => DateTime.Parse(i.Date)).ToList();
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

    internal void AnalyseMatches()
    {
        _poissonService = new PoissonService(_historicalGames);
        var service = new CalculationService(_poissonService, _historicalGames, _upComingGames);
        
        
      service.Execute();
        
         var dec = new DecisionTree();
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

        var cal = new CalculationService(service, _historicalGames, _upComingGames);
        cal.Execute(homeTeam, awayTeam, league);
        
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
            .Where(ia => ia.Key != nameof(GameData.AwayWin) && ia.Key != nameof(GameData.HomeWin) &&
                         ia.Key != nameof(GameData.Draw))
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
                case nameof(GameData.BothTeamScore):
                    if (item.Probability > 0.64)
                    {
                        var msg = $"{date:d} {time:g}: {match} Both score = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(GameData.MoreThanTwoGoals):
                    if (item.Probability > 0.58)
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
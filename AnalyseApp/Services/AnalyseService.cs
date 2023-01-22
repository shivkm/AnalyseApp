using System.Globalization;
using AnalyseApp.Commons.Constants;
using AnalyseApp.Extensions;
using AnalyseApp.models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class AnalyseService
{
    private const string FileDir = "/Users/shivm/Documents/projects/AnalyseApp/data";
    private List<GameData> _historicalGames = new();
    private List<GameData> _upComingGames = new();
    private IPoissonService _poissonService = new PoissonService(null, null);

    internal AnalyseService ReadHistoricalGames()
    {
        var files = Directory.GetFiles($"{FileDir}/raw_csv");

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

    internal AnalyseService ReadUpcomingGames()
    {
        var files = Directory.GetFiles($"{FileDir}/upcoming_matches");
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
        var cal = new CalculationService(_historicalGames, _upComingGames);
        var dec = new DecisionTree();
        _poissonService = new PoissonService(_historicalGames, _upComingGames);
        foreach (var upComingGame in _upComingGames)
        {
           // dec.Train(_historicalGames, upComingGame.HomeTeam, upComingGame.AwayTeam, upComingGame.Div);
           // dec.Predict(new[] { "FTR" });
            var analysePoisson = _poissonService.Execute(
                upComingGame.HomeTeam,
                upComingGame.AwayTeam,
                upComingGame.Div
            );

            PickTheBestProbability(upComingGame.HomeTeam,
                upComingGame.AwayTeam, analysePoisson, Convert.ToDateTime(upComingGame.Date), Convert.ToDateTime(upComingGame.Time).TimeOfDay);
        }
    }

    internal void Analyse(string homeTeam, string awayTeam, string league)
    {
        var service = new CalculationService(_historicalGames, _upComingGames);
        service.Execute(homeTeam, awayTeam, league);

     //  PickTheBestProbability(homeTeam, awayTeam, analysePoisson);
    
    
    }

    private void PickTheBestProbability(string homeTeam, string awayTeam, IEnumerable<PoissonProbability> analysePoisson, DateTime date = default, TimeSpan time = default)
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
            .Take(3)
            .ToList();

        probabilitiesInDescOrder.ForEach(item =>
        {
            var match = $"{homeTeam}:{awayTeam}";
            if (DerbyTeams.GetDerbyMatches().Contains(match))
                match = $"DERBY MATCH!! {match}";
            
            switch (item.Key)
            {
                case nameof(GameData.BothTeamScore):
                    if (item.Probability > 0.55)
                        Console.WriteLine($"{date:d} {time:g}: {match} Both score = {item.Probability * 100}%");
                    break;
                case nameof(GameData.MoreThanTwoGoals):
                    if (item.Probability > 0.55)
                        Console.WriteLine($"{date:d} {time:g}: {match} More Than two goals = {item.Probability * 100}%");
                    break;
                case nameof(GameData.TwoToThree):
                    if (item.Probability > 0.60)
                        Console.WriteLine($"{date:d} {time:g}: {match} Two to three goals = {item.Probability * 100}%");
                    break;
                case nameof(GameData.LessThanTwoGoals):
                    if (item.Probability > 0.68)
                        Console.WriteLine($"{date:d} {time:g}: {match} less than three goals = {item.Probability * 100}%");
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
                        Console.WriteLine($"{date:d} {time:g}: {match} Home win = {item.Probability * 100}%");
                    break;
                case nameof(GameData.AwayWin): 
                    if (item.Probability >= 0.55)
                        Console.WriteLine($"{date:d} {time:g}: {match} Away win = {item.Probability * 100}%");
                    break;
                case nameof(GameData.Draw): 
                    if (item.Probability >= 0.60)
                        Console.WriteLine($"{date:d} {time:g}: {match} Draw = {item.Probability * 100}%");
                    break;
            }
            
        });
    }
}
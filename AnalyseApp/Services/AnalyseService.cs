using System.Globalization;
using AnalyseApp.Extensions;
using AnalyseApp.models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class AnalyseService
{
    private const string FileDir = "C:\\shivm\\AnalyseApp\\data";
    private List<GameData> _historicalGames = new();
    private List<GameData> _upComingGames = new();
    private IPoissonService _poissonService = new PoissonService(null, null);

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
        _poissonService = new PoissonService(_historicalGames, _upComingGames);
        foreach (var upComingGame in _upComingGames)
        {
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
        _poissonService = new PoissonService(_historicalGames, _upComingGames);
        var analysePoisson = _poissonService.Execute(homeTeam, awayTeam, league);

       PickTheBestProbability(homeTeam, awayTeam, analysePoisson);


       // Create the machine learning context
        var context = new MLContext();

     
        
        // Convert the data to an IDataView
       
        // Define the pipeline
        var pipeline = context.Transforms.Conversion.MapValueToKey("Label", "Score")
            .Append(context.Transforms.Categorical.OneHotEncoding("HomeTeamWinsOneHot", "HomeTeamWins"))
            .Append(context.Transforms.Concatenate("Features", 
                "HomeTeamGoals", "AwayTeamGoals", "HomeTeamHalfTimeGoals",
                "AwayTeamHalfTimeGoals", "HomeTeamWinsOneHot"))
            .Append(context.Transforms.NormalizeMinMax("Features"))
            .Append(context.Transforms.Conversion.MapKeyToValue("Label"))
            .Append(context.Regression.Trainers.Sdca());

        // Train the model

        
        
        
        
        
      /*  
        // Create a new MLContext
        var context = new MLContext();

        // Read the data from the CSV file

        var data = _gameData
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam ||
                        i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .Select(i => new FootballGameData
            {
                HomeTeamGoals = Convert.ToSingle(i.FTHG),
                AwayTeamGoals = Convert.ToSingle(i.FTAG),
                HomeTeamHalfTimeGoals = Convert.ToSingle(i.HTHG),
                AwayTeamHalfTimeGoals = Convert.ToSingle(i.HTAG),
                HomeTeamWins = i.FTR == "H",
                Score = new[] { Convert.ToSingle(i.FTHG), Convert.ToSingle(i.FTAG) }

            })
            .ToList();
        
        if (!data.Any())
           return;
        // Convert the data to an IDataView
        var gameDataView = context.Data.LoadFromEnumerable(data);

        // Define the pipeline
        var pipeline =
            context.Transforms.Conversion.MapValueToKey("Score")
                .Append(context.Transforms.Categorical.OneHotEncoding("HomeTeamWinsOneHot", "HomeTeamWins"))
                .Append(context.Transforms.Concatenate(
                    "Features", "HomeTeamGoals", "AwayTeamGoals",
                    "HomeTeamHalfTimeGoals", "AwayTeamHalfTimeGoals", "HomeTeamWinsOneHot"))
                .Append(context.Transforms.NormalizeMinMax("Features"))
                .Append(context.Regression.Trainers.Sdca())
                .Append(context.Transforms.Conversion.MapKeyToValue("Score", "Label"));

        
        // Train the model
        var model = pipeline.Fit(gameDataView);

        var prediction = context.Model.CreatePredictionEngine<FootballGameData, FootballGamePrediction>(model)
            .Predict(new FootballGameData { HomeTeamGoals = 2, AwayTeamGoals = 1, HomeTeamWins = true });
      
        Console.WriteLine($"Home Team Goals: {prediction.Score[0]} Away Team Goals: {prediction.Score[1]}");*/
    
    }

    private static void PickTheBestProbability(string homeTeam, string awayTeam, IEnumerable<MatchProbability> analysePoisson, DateTime date = default, TimeSpan time = default)
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
            switch (item.Key)
            {
                case nameof(GameData.BothTeamScore):
                    if (item.Probability > 0.60)
                        Console.WriteLine($"{date} {time}: {homeTeam}:{awayTeam} Both score = {item.Probability * 100}%");
                    break;
                case nameof(GameData.MoreThanTwoGoals):
                    if (item.Probability > 0.55)
                        Console.WriteLine($"{date} {time}: {homeTeam}:{awayTeam} More Than two goals = {item.Probability * 100}%");
                    break;
                case nameof(GameData.TwoToThree):
                    if (item.Probability > 0.60)
                        Console.WriteLine($"{date} {time}: {homeTeam}:{awayTeam} Two to three goals = {item.Probability * 100}%");
                    break;
                case nameof(GameData.LessThanTwoGoals):
                    if (item.Probability > 0.68)
                        Console.WriteLine($"{date} {time}: {homeTeam}:{awayTeam} less than three goals = {item.Probability * 100}%");
                    break;
            }
        });
        
        oddProbabilitiesInDescOrder.ForEach(item =>
        {
            switch (item.Key)
            {
                case nameof(GameData.HomeWin):
                    if (item.Probability > 0.70)
                        Console.WriteLine($"{date} {time}: {homeTeam}:{awayTeam} Home win = {item.Probability * 100}%");
                    break;
                case nameof(GameData.AwayWin): 
                    if (item.Probability >= 0.40)
                        Console.WriteLine($"{date} {time}: {homeTeam}:{awayTeam} Away win = {item.Probability * 100}%");
                    break;
                case nameof(GameData.Draw): 
                    if (item.Probability >= 0.60)
                        Console.WriteLine($"{date} {time}: {homeTeam}:{awayTeam} Draw = {item.Probability * 100}%");
                    break;
            }
            
        });
    }
}
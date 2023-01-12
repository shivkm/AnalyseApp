using AnalyseApp.Extensions;
using AnalyseApp.models;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class AnalyseService
{
    private readonly List<GameData> _gameData;

    public AnalyseService(List<GameData> gameData)
    {
        _gameData = gameData;
    }


    
    public void AnalysePattern(string homeTeam, string awayTeam)
    {
        
        // Create the machine learning context
        var context = new MLContext();

        // Read the data from the input file
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
                Score = new [] { Convert.ToSingle(i.FTHG), Convert.ToSingle(i.FTAG) }
            })
            .ToList();
        
        // Convert the data to an IDataView
        var gameDataView = context.Data.LoadFromEnumerable(data);

        // Define the pipeline
        var pipeline = context.Transforms.Conversion.MapValueToKey("Label", "Score")
            .Append(context.Transforms.Categorical.OneHotEncoding("HomeTeamWinsOneHot", "HomeTeamWins"))
            .Append(context.Transforms.Concatenate("Features", 
                "HomeTeamGoals", "AwayTeamGoals", "HomeTeamHalfTimeGoals",
                "AwayTeamHalfTimeGoals", "HomeTeamWinsOneHot"))
            .Append(context.Transforms.NormalizeMinMax("Features"))
            .Append(context.Transforms.Conversion.MapKeyToValue("Label"))
            .Append(context.MulticlassClassification.Trainers.SdcaNonCalibrated());

        // Train the model
        var model = pipeline.Fit(gameDataView);

        var prediction = context.Model.CreatePredictionEngine<FootballGameData, FootballGamePrediction>(model)
            .Predict(new FootballGameData { HomeTeamGoals = 2, AwayTeamGoals = 1, HomeTeamWins = true });
      
        Console.WriteLine($"Home Team Goals: {prediction.Score[0]} Away Team Goals: {prediction.Score[1]}");
        
        
        
        
        
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
    
    public Game AnalyseGameBy(string homeTeam, string awayTeam, DateTime dateTime)
    {
        var headToHead = _gameData.AnalyseHeadToHead(homeTeam, awayTeam);
        var lastTwelveGames = AnalyseGames(homeTeam, awayTeam, 12);
        var lastSixGames = AnalyseGames(homeTeam, awayTeam, 6);
        var allGames = AnalyseGames(homeTeam, awayTeam, 0);
        var result = new Game
        {
            Title = $"{dateTime} {homeTeam}:{awayTeam}",
            LastTwelveGames = lastTwelveGames,
            LastSixGames = lastSixGames,
            AllGame = allGames,
            HeadToHead = headToHead
        };

        return result;
    }
    
    private GameAverage AnalyseGames(string homeTeam, string awayTeam, int takeLastGames)
    {
        var nextHomeGame = new NextGame(takeLastGames, 60, 55)
        {
            Team = homeTeam,
            Msg = default!,
            IsHome = true
        };
        var nextAwayGame = new NextGame(takeLastGames, 60, 55)
        {
            Team = awayTeam,
            Msg = default!,
            IsHome = default
        };
        var home = _gameData
            .GetGamesDataBy(nextHomeGame)
            .AnalyseTeamGoals(nextHomeGame);
        
        var away = _gameData
            .GetGamesDataBy(nextAwayGame)
            .AnalyseTeamGoals(nextAwayGame);

        var result = new GameAverage
        {
            Home = home,
            Away = away
        };
        
        return result;
    }
}
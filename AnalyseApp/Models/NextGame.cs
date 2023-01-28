namespace AnalyseApp.Models;

public record NextGame(
    int TakeLastGames,
    int ExpectedPercentageForOneGoal, 
    int ExpectedPercentageForTwoGoal
)
{
    public required string Team { get; set; }
    public required string Msg { get; set; }
    public bool IsHome { get; set; }
};


/*
 * 
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
                HomeTeamWins = i.FTR == "H"

            });
        var dataView = context.Data.LoadFromEnumerable(data);
        // Descibe the approch 
        // Tms demo

        // Define the pipeline
        var pipeline = context.Transforms.Categorical.OneHotEncoding("HomeTeamWinsOneHot", "HomeTeamWins")
            .Append(context.Transforms.Concatenate(
                "Features", "HomeTeamGoals", "AwayTeamGoals", "HomeTeamHalfTimeGoals",
                "AwayTeamHalfTimeGoals", "HomeTeamWinsOneHot"))
            .Append(context.Regression.Trainers.FastTree())
            .Append(context.Transforms.Conversion.MapKeyToValue("Score", "Label"));

        // Train the model
        var model = pipeline.Fit(dataView);

        var prediction = context.Model.CreatePredictionEngine<FootballGameData, FootballGamePrediction>(model)
            .Predict(new FootballGameData { HomeTeamGoals = 2, AwayTeamGoals = 1, HomeTeamWins = true });
        
        Console.WriteLine($"Home Team Goals: {prediction.Score[0]} Away Team Goals: {prediction.Score[1]}");
 */
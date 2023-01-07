using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using AnalyseApp.Extensions;
using AnalyseApp.models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace AnalyseApp;

public class Analyse
{
    private const string FileDir = "C:\\shivm\\AnalyseApp\\data";
    private List<Matches> _historicalGames = new();
    private List<Matches> _upComingGames = new();
    private const int TeamPerformancePassingPercentage = 60;
    
    internal Analyse ReadFilesHistoricalGames()
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

            var currentFileGames = csv.GetRecords<Matches>();
            _historicalGames.AddRange(currentFileGames);
        }

        _historicalGames = _historicalGames.OrderByDescending(i => i.Date).ToList();
        return this;
    }
    
    internal Analyse ReadUpcomingGames()
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

            var currentFileGames = csv.GetRecords<Matches>();

            _upComingGames.AddRange(currentFileGames);
        }

        _upComingGames = _upComingGames.OrderByDescending(i => i.Date).ToList();
        
        return this;
    }

    internal async Task CreateCsvFile()
    {
        var files = Directory.GetFiles($"{FileDir}\\raw_csv");
        var records = new List<GameData>();
        foreach (var file in files)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, config);

            var currentFileGames = csv.GetRecords<GameData>();
            records.AddRange(currentFileGames);
        }
        await using var writer = new StreamWriter($"{FileDir}\\ml\\onefile.csv");
        await using var csvfile = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        await csvfile.WriteRecordsAsync(records);
    }


    private float ExecuteDecisionTree(string homeTeam, string awayTeam)
    {
        // Create a new ML context
        var mlContext = new MLContext();

        var matches = _historicalGames
            .Where(i => i.AwayTeam == awayTeam || i.HomeTeam == homeTeam || i.HomeTeam == homeTeam || i.AwayTeam == awayTeam)
            .Select(s => new GameData
            {
                HomeGoals = Convert.ToSingle(s.FTHG),
                AwayGoals = Convert.ToSingle(s.FTHG),
                HalfTimeHomeGoals = Convert.ToSingle(s.FTHG),
                HalfTimeAwayGoals = Convert.ToSingle(s.FTHG)
            })
            .ToList();

        if (!matches.Any()) return 0;
        
        
        // Load the training data
        var trainingData = mlContext.Data.LoadFromEnumerable(matches);

        
        // 2. Specify data preparation and model training pipeline
        var pipeline = mlContext.Transforms.Concatenate(
                "Features", new [] { "HomeGoals", "AwayGoals", "HalfTimeHomeGoals", "HalfTimeAwayGoals" } )
            .Append(mlContext.Regression.Trainers.Sdca(labelColumnName: "HomeGoals", maximumNumberOfIterations: 1000));

        // 3. Train model
        var model = pipeline.Fit(trainingData);

        // 4. Make a prediction
        var size = new GameData { HomeGoals = 1, HalfTimeHomeGoals = 1 };
     //   var price = mlContext.Model.CreatePredictionEngine<GameData, Prediction>(model).Predict(size);

        return 0;
    }
    

    internal int ExecuteDecisionTree3(string homeTeam, string awayTeam)
    {
        var mlContext = new MLContext();

        var data = _historicalGames
            .Where(i => i.AwayTeam == awayTeam || i.HomeTeam == homeTeam || i.HomeTeam == homeTeam || i.AwayTeam == awayTeam)
            .Select(s => new DataPoint
            {
                Features = new float[] { Convert.ToSingle(s.HTHG), Convert.ToSingle(s.HTAG) },
                Label = Convert.ToSingle(s.FTHG) > 0 && Convert.ToSingle(s.FTAG) > 0 ? 1 : 0
            })
            .ToList();

        if (!data.Any()) return 0;
        
        var loadData = mlContext.Data.LoadFromEnumerable(data);
        // Split the data into training and test sets
        var splitData = mlContext.Data.TrainTestSplit(loadData, testFraction: 0.2);
        var trainingData = splitData.TrainSet;
        var testData = splitData.TestSet;
        
        // Build the training pipeline
        var trainer = mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: "Label", featureColumnName: "Features");
            
        var trainingPipeline = trainer.Append(mlContext.Transforms.Conversion.MapKeyToValue(
            "PredictedLabel", "PredictedLabel"));

        Console.WriteLine("Training the model...");
        var model = trainingPipeline.Fit(trainingData);

        // Use the model to make predictions
        Console.WriteLine("Making predictions...");
        var predictions = model.Transform(trainingData);
        var predictedLabels = mlContext.Data.CreateEnumerable<Prediction>(predictions, reuseRowObject: false);

        
        return 0;
    }
    
    internal void StartAnalysis()
    {
        var upComingMatches = new List<NextMatch>();
        foreach (var upComingGame in _upComingGames)
        {
            var nextMatch = new NextMatch
            {
                HomeTeam = upComingGame.HomeTeam, 
                AwayTeam = upComingGame.AwayTeam, 
                Date = DateTime.Parse(upComingGame.Date)
            };

            Execute(nextMatch);
          //  nextMatch.PredictATargetUsingALinearRegressionModel = $"{ExecuteDecisionTree(upComingGame.HomeTeam, upComingGame.AwayTeam)}%";
            upComingMatches.Add(nextMatch);
            
            //ExecuteDecisionTree3(upComingGame.HomeTeam, upComingGame.AwayTeam);
        }

        upComingMatches.FindTopMatchesForBothTeamScore(70);
        upComingMatches.FindTopMatchesForMoreThanTwoGoal(45);
    }

    private void Execute(NextMatch nextMatch)
    {
        var currentSeasonMatches = _historicalGames.GetCurrentSeasonBy();
        nextMatch.HomeCurrentAverage = currentSeasonMatches.TeamPerformance(nextMatch.HomeTeam, true, TeamPerformancePassingPercentage);
        nextMatch.AwayCurrentAverage = currentSeasonMatches.TeamPerformance(nextMatch.AwayTeam, false, TeamPerformancePassingPercentage);

        var lastFiveMatches = currentSeasonMatches.OrderByDescending(i => i.Date).TakeLast(5);
        var lastFiveMatchesHome = currentSeasonMatches.TeamPerformance(nextMatch.HomeTeam, true, TeamPerformancePassingPercentage);
        var lastFiveMatchesAway = currentSeasonMatches.TeamPerformance(nextMatch.AwayTeam, false, TeamPerformancePassingPercentage);
        
        nextMatch.HomeOverAllAverage = _historicalGames.TeamPerformance(nextMatch.HomeTeam, true, TeamPerformancePassingPercentage);
        nextMatch.AwayOverAllAverage = _historicalGames.TeamPerformance(nextMatch.AwayTeam, false, TeamPerformancePassingPercentage);

        nextMatch.CurrentHeadToHeadAverage = currentSeasonMatches.AnalyseHeadToHeadPerformance(nextMatch.HomeTeam, nextMatch.AwayTeam);
        nextMatch.HeadToHeadAverage = _historicalGames.AnalyseHeadToHeadPerformance(nextMatch.HomeTeam, nextMatch.AwayTeam);

        if (nextMatch.HomeOverAllAverage?.AtLeastOneGoal == 0 || nextMatch.AwayOverAllAverage?.AtLeastOneGoal == 0)
            return;
        
        nextMatch.GenerateOutput();
    }
}



/*
 * Predicting the score of a football match can be a challenging task, as there are many factors that can impact the outcome of the game. Here are a few tips that might be helpful:

Analyze the teams: Look at the strengths and weaknesses of each team, and consider how they might match up against each other. Take into account factors like the team's current form, their past performance against each other, and any injuries or other absences.

Look at the context of the match: Consider the importance of the match and any external factors that might affect the teams' performance. For example, a team might be more motivated to win if they are fighting for a spot in a tournament or trying to avoid relegation.

Consider the conditions: Think about how the weather and the state of the pitch might affect the teams' strategies and performance.

Make use of statistical models: There are a number of statistical models that can be used to predict the outcome of football matches. These models can take into account a variety of factors, including the teams' past performance and the importance of the match.

Get expert opinions: Look for analysis and predictions from experts in the field, such as journalists, former players, and coaches.

I hope that helps! Predicting the score of a football match is always going to involve some level of uncertainty, but by considering a range of factors, you can increase your chances of making an informed prediction.

There are several ways you can use machine learning (ML) to predict the outcome of a football match. Here are a few approaches you might consider:

Collect data on past football matches, including information about the teams, players, match conditions, and match outcomes. This data can be used to train a machine learning model to predict the outcome of future matches.

Use statistical modeling techniques to analyze the data and identify patterns that may be predictive of match outcomes. This could include analyzing the strength of each team's defense, the scoring abilities of their players, or the impact of home field advantage.

Train a machine learning model on the data, using algorithms such as decision trees, random forests, or neural networks. These models can learn from the data and make predictions about the likelihood of different outcomes for future matches.

Evaluate the performance of the machine learning model using metrics such as accuracy, precision, and recall. You may need to fine-tune the model by adjusting its hyper parameters or by collecting more data in order to improve its performance.

Use the model to make predictions about the outcomes of future matches. You can also use the model to identify factors that are most important in determining the outcome of a match, which can help you understand why the model is making certain predictions.

There are many different types of data you can collect about football teams and their players that could be useful for a machine learning model to predict the outcome of a match. Some examples of data you might consider collecting include:

Team statistics: This could include data on the team's overall record (wins, losses, draws), goals scored and allowed, and other metrics that reflect the team's performance.

Player statistics: Data on the performance of individual players can be useful, such as goals scored, assists, tackles, and other metrics that reflect the player's contributions to the team.

Team and player ratings: You could also consider collecting data on the ratings of teams and players from various sources, such as sports websites or expert analysts. These ratings could reflect the overall strength of a team or the quality of individual players.

Match conditions: Data on the conditions of the match, such as the location (home field or away), the weather, and the surface of the field, could also be useful for predicting the outcome of a match.

Injuries: Information on the availability of key players, particularly due to injuries, could also be useful for predicting the outcome of a match.

It's important to note that the specific data you collect will depend on the goals of your model and the questions you are trying to answer. You may need to experiment with different types of data and features to find the combination that works best for your model.

 */
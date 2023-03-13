using MathNet.Numerics.Distributions;
using Microsoft.ML;
using Microsoft.ML.Data;
using PredictionTool.Extensions;
using PredictionTool.Interfaces;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class FilterService: IFilterService
{
    private const string LessThanTwoGoals = "LessThanTwoGoals";
    private const string TwoToThree = "TwoToThree";
    private const string BothTeamScore = "BothTeamScore";
    private const string MoreThanTwoGoals = "MoreThanTwoGoals";
    private const string Score = "Score";
    private const string ZeroGoal = "ZeroGoal";
    private const double MinScored = 0.60;
    private const double MaxConceded = 1.20;
    private const double MinProbability = 0.68;
    
    public FilterService() { }
    
    public (string Key, double Probability) FilterGames(
        QualifiedGames qualifiedGames, List<GameProbability> gameProbabilities, List<Game> historicalGames)
    {
        // Filter current season games
        var games = historicalGames.GetCurrentSeasonGamesBy(2022, 2023, qualifiedGames.League);
        
        // current form of both teams
        var teamFormCalculator = new TeamFormCalculator(historicalGames);
        var home = teamFormCalculator.CalculateForm(qualifiedGames.Home, 6);
        var away = teamFormCalculator.CalculateForm(qualifiedGames.Away, 6);
        
        
        
        var currentForms = CalculateTeamsCurrentFormBy(qualifiedGames.Home, qualifiedGames.Away, historicalGames);

        // head to head accuracy
        var headToHeads = CalculateHeadToHeadBy(qualifiedGames.Home, qualifiedGames.Away, historicalGames);

        // calculate the team score mode
        var teamsMode = CalculateModeBy(qualifiedGames.Home, qualifiedGames.Away, games);

        //AnalyseNeuralNetworkBy(qualifiedGames.Home, historicalGames);
        //AnalyseNeuralNetworkBy(qualifiedGames.Away, historicalGames);
        //var linearReg = GetLinearClassificationAnalysisBy(qualifiedGames.Home, qualifiedGames.Away, historicalGames);
        //GoOverClassificationsBy(historicalGames, qualifiedGames.Home);
        //GoOverClassificationsBy(historicalGames, qualifiedGames.Away);
        // markov chain average used to calculate poisson prabability
        var homeMarkovChain = TeamMarkovChainProbability(historicalGames, qualifiedGames.Home);
        var awayMarkovChain = TeamMarkovChainProbability(historicalGames, qualifiedGames.Away);

        var poissonProb = gameProbabilities
            .OrderByDescending(i => i.Probability)
            .ToList();
        if (qualifiedGames.Home == "Swansea" )
        {
            
        }
        var overTwoGoals = BothScoreAnalysisBy(currentForms, headToHeads, teamsMode);
        if (overTwoGoals.Qualified && homeMarkovChain > MinScored + 0.05 && awayMarkovChain > MinScored + 0.05)
        {
            Console.WriteLine($"{qualifiedGames.DateTime} {qualifiedGames.Home}:{qualifiedGames.Away} {overTwoGoals.Key} {
                poissonProb.MaxBy(i => i.Probability)?.Key}");
        }
        else
        {
            Console.WriteLine($"Failed {qualifiedGames.DateTime} {qualifiedGames.Home}:{qualifiedGames.Away} {overTwoGoals.Key} {
                poissonProb.MaxBy(i => i.Probability)?.Key}");   
        }
        
        
        
        return ("", 0);
    }

/*
    private static (bool Suitable, string Key) TacticsAndWeatherAnalysisBy(TeamForm currentForm, Weather weather, string tactic)
    {
        // Check if the team is in good form
        if (currentForm is { GoalPerformance: > MinProbability, HomeForm: "W" })
        {
            // Check if the weather is suitable for the tactic
            if (tactic == "attacking" && weather is { Temperature: >= 15, WindSpeed: < 10 })
            {
                return (true, "Attacking tactic suitable");
            }
            else if (tactic == "defensive" && weather is { Temperature: < 15, WindSpeed: > 10 })
            {
                return (true, "Defensive tactic suitable");
            }
        }
    
        return (false, "");
    }*/

    private void GoOverClassificationsBy(List<Game> historicalData, string team)
    {
        // Filter historical games for the given teams
        var games = historicalData
            .Where(i => i.Home == team || i.Away == team)
            .OrderByDescending(o => o.DateTime)
            .ToList();

        // Create MLContext
        var mlContext = new MLContext();
        
        // Load data into memory
        var data = mlContext.Data.LoadFromEnumerable(games.Select(s => new GameData
        {
            Home = s.Home,
            Away = s.Away,
            HomeScore = s.FullTimeHomeScore ?? 0,
            AwayScore = s.FullTimeAwayScore ?? 0,
            HalftimeHomeGoal = s.HalftimeHomeScore ?? 0,
            HalftimeAwayGoal = s.HalftimeAwayScore ?? 0,
            TotalGoals = s.FullTimeHomeScore + s.FullTimeAwayScore ?? 0,
            Label = s.FullTimeHomeScore + s.FullTimeAwayScore > 2 ? 1 : 0
        }));

        // Split data into training and testing sets
        var trainTestSplit = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);
        var trainData = trainTestSplit.TrainSet;
        var testData = trainTestSplit.TestSet;

        // Train and evaluate logistic regression model
        var lrModel = TrainAndEvaluateModel(mlContext, trainData, testData, "SdcaLogisticRegression");

        // Train and evaluate support vector machines model
        var svmModel = TrainAndEvaluateModel(mlContext, trainData, testData, "LinearSvm");

        // Train and evaluate random forest tree model
        var dtModel = TrainAndEvaluateModel(mlContext, trainData, testData, "RandomForest");

        // Train and evaluate fast tree model
        var ftModel = TrainAndEvaluateModel(mlContext, trainData, testData, "FastTree");

        var lrPrediction = Predict(mlContext, lrModel, new Game { FullTimeHomeScore = 1, FullTimeAwayScore = 0, HalftimeHomeScore = 1, HalftimeAwayScore = 0 });
        Console.WriteLine($"{team} Logistic Regression Prediction: {lrPrediction}");

        // Predict using support vector machines model
        var svmPrediction = Predict(mlContext, svmModel, new Game { FullTimeHomeScore = 1, FullTimeAwayScore = 0, HalftimeHomeScore = 1, HalftimeAwayScore = 0 });
        Console.WriteLine($"{team} Support Vector Machines Prediction: {svmPrediction}");

        var dtPrediction = Predict(mlContext, dtModel, new Game { FullTimeHomeScore = 1, FullTimeAwayScore = 0, HalftimeHomeScore = 1, HalftimeAwayScore = 0 });
        Console.WriteLine($"{team} Logistic Regression Prediction: {dtPrediction}");

        // Predict using support vector machines model
        var ftPrediction = Predict(mlContext, ftModel, new Game { FullTimeHomeScore = 1, FullTimeAwayScore = 0, HalftimeHomeScore = 1, HalftimeAwayScore = 0 });
        Console.WriteLine($"{team} Support Vector Machines Prediction: {ftPrediction}");

    }

    private bool Predict(MLContext mlContext, ITransformer model, Game game)
    {
        // Create prediction engine
        var engine = mlContext.Model.CreatePredictionEngine<GameData, Prediction>(model);

        // Convert input game to GameData
        var data = new GameData
        {
            Home = game.Home,
            Away = game.Away,
            HomeScore = game.FullTimeHomeScore ?? 0,
            AwayScore = game.FullTimeAwayScore ?? 0,
            HalftimeHomeGoal = game.HalftimeHomeScore ?? 0,
            HalftimeAwayGoal = game.HalftimeAwayScore ?? 0 ,
            TotalGoals = game.FullTimeHomeScore + game.FullTimeAwayScore ?? 0,
            Label = game.FullTimeHomeScore + game.FullTimeAwayScore > 2 ? 1 : 0
        };

        // Predict outcome using model
        var prediction = engine.Predict(data);

        // Return prediction
        return prediction.Predict;
    }

    private ITransformer TrainAndEvaluateModel(MLContext mlContext, IDataView trainData, IDataView testData,
        string modelName)
    {
        Console.WriteLine($"Training {modelName} model...");
        // Define data preparation pipeline
        var dataPipeline = mlContext.Transforms
            .Concatenate("Features", "HomeScore", "AwayScore", "HalftimeHomeGoal", "HalftimeAwayGoal", "TotalGoals")
            .Append(mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(mlContext.Transforms.Conversion.ConvertType("Label", "Label", DataKind.Boolean));

        // Define trainer algorithm
        IEstimator<ITransformer> trainer = modelName switch
        {
            "SdcaLogisticRegression" => mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(),
            "LinearSvm" => mlContext.BinaryClassification.Trainers.LinearSvm(),
            "RandomForest" => mlContext.BinaryClassification.Trainers.FastForest(),
            "FastTree" => mlContext.BinaryClassification.Trainers.FastTree(),
            _ => throw new ArgumentException($"Invalid model name:")
        };

        // Define the training pipeline
        var trainingPipeline = dataPipeline.Append(trainer);

        // Train the model
        var trainedModel = trainingPipeline.Fit(trainData);

        // Evaluate the model
        var predictions = trainedModel.Transform(testData);
        var metrics = mlContext.BinaryClassification.Evaluate(predictions, "Label");

        Console.WriteLine($"Evaluation Metrics for {modelName} model");
        Console.WriteLine($"  Accuracy: {metrics.Accuracy}");
        Console.WriteLine($"  F1Score: {metrics.F1Score}");

        return trainedModel;
    }

    private static (bool Qualified, string Key) BothScoreAnalysisBy(
        TeamAccuracy currentForm, HeadToHead headToHead, (int home, int away) teamMode)
    {
        if (currentForm is 
            {
                HomeScoreProbability: > MinProbability,
                AwayScoreProbability: > MinProbability
            } and
            {
                HomeScoredGameAvg: > MinScored,
                AwayScoredGameAvg: > MinScored
            })
        {
            if (currentForm.HomeHalftimeScoreProbability > 0.50  || 
                currentForm is { AwayHalftimeScoreProbability: > 0.50, HomeShotsOnGoalsAvg: > 0.60 } ||
                currentForm.AwayShotsOnGoalsAvg > 0.60 &&
                teamMode is { home: > 0, away: > 0 } && headToHead is { PlayedMatches: > 3, BothScoredAvg: > 0.50 })
            {
                return (true, "Both team score");
            }
        }
        return (false, "");
    }
    /*
    private static (bool Qualified, string Key) OverTwoGoalsAnalysisBy(TeamAccuracy currentForm, HeadToHead headToHead, (int home, int away) teamMode)
    {
        if (currentForm is { HomeScoreProbability: > MinScored + 0.1, AwayScoreProbability: > MinScored + 0.1 } and
            { AwayLastFiveOver: false, HomeLastFiveOver: false })
        {
            if (headToHead is { PlayedMatches: >= 4, MoreThanTwoGoalsAvg: >= MinScored })
            {
                return (true, "Over 2.5 Goals");
            }

            if (currentForm is not { HomeScoreProbability: > MinScored + 0.2, AwayScoreProbability: > MinScored + 0.2 } && 
                    teamMode is { home: > 0, away: 0 } or { home: 0, away: > 0 })
            {
                return (true, "Two to three goals");
            }
            if (currentForm is { HomeScoreProbability: > MinScored + 0.2, AwayScoreProbability: > MinScored + 0.2 } and
                { AwayLastFiveOver: false, HomeLastFiveOver: false })
            {
                return (true, "Over 2.5 Goals");
            }
        }
        if (currentForm is { HomeScoreProbability: > MinScored + 0.1, AwayScoreProbability: > MinScored + 0.1 } and 
            { AwayLastFiveOver: false, HomeLastFiveOver: false })
        {
            if (teamMode is { home: > 0, away: > 0 })
            {
                return (true, "Both team score");
            }
            
            if (headToHead is { PlayedMatches: >= 4, BothScoredAvg: >= MinScored })
            {
                return (true, "Both team score");
            }
        }
        if (currentForm is { AwayScoredAvg: < MinScored, AwayConcededAvg: > MaxConceded })
        {
            if (currentForm.HomeScoreProbability > currentForm.AwayScoreProbability &&
                currentForm is { HomeScoreProbability: >= MinScored + 0.1, AwayScoreProbability: < MinScored, HomeLastFiveWon: false })
            {
                return (true, "Home win the match");
            }
        }
        if (currentForm is { HomeScoredAvg: < MinScored, HomeConcededAvg: > MaxConceded })
        {
            if (currentForm.AwayScoreProbability > currentForm.HomeScoreProbability &&
                currentForm is { AwayScoreProbability: >= MinScored + 0.1, HomeScoreProbability: < MinScored, AwayLastFiveOver: false  })
            {
                return (true, "Away win the match");
            }
        }
        
        if (currentForm is { HomeScoredAvg: <= MinScored, HomeConcededAvg: < MaxConceded } or 
                            {AwayScoredAvg: <= MinScored, AwayConcededAvg: < MaxConceded})
        {
            if (currentForm is { AwayScoreProbability: <= MinScored, HomeScoreProbability: <= MinScored } &&
                headToHead is { PlayedMatches: > 2, MoreThanTwoGoalsAvg: < MinScored - 0.1 })
            {
                return (true, "less than three goals");
            }
        }

        return (false, "");
    }
    */
    private static TeamAccuracy CalculateTeamsCurrentFormBy(string home, string away, List<Game> games)
    {
        // Last Six home and away games
        var homeCurrentGames = games.GetLastSixGamesBy(home);
        var awayCurrentGames = games.GetLastSixGamesBy(away);
        
        // Calculate the Average of home and away times
        var homeScoredAvg = homeCurrentGames.CalculateScoredGoalAccuracy(home);
        var homeConcededAvg = homeCurrentGames.CalculateConcededGoalAccuracy(home);
        var awayScoredAvg = awayCurrentGames.CalculateScoredGoalAccuracy(away);
        var awayConcededAvg = awayCurrentGames.CalculateConcededGoalAccuracy(away);
        var homeHalftimeScoreAvg = homeCurrentGames.CalculateHalftimeScoreGoalAccuracy(home);
        var homeHalftimeConcededAvg = awayCurrentGames.CalculateHalftimeScoreGoalAccuracy(home);
        var awayHalftimeScoreAvg = awayCurrentGames.CalculateHalftimeScoreGoalAccuracy(away);
        var awayHalftimeConcededAvg = awayCurrentGames.CalculateHalftimeScoreGoalAccuracy(away);
        var homeShotsAvg = homeCurrentGames.CalculateShotsoncededGoalAccuracy(home);
        var awayShotsAvg = awayCurrentGames.CalculateShotsoncededGoalAccuracy(away);
        
        // calculate the final average
        var homeFinalScoreAvg = homeScoredAvg * awayConcededAvg;
        var awayFinalScoreAvg = awayScoredAvg * homeConcededAvg;
        var homeFinalHalftimeScoreAvg = homeHalftimeScoreAvg * awayHalftimeConcededAvg;
        var awayFinalHalftimeScoreAvg = awayHalftimeScoreAvg * homeHalftimeConcededAvg;

        // calculate poisson probability of scoring goal
        var homeScoreProbability = CalculateScoreProbabilityBy(homeFinalScoreAvg);
        var awayScoreProbability = CalculateScoreProbabilityBy(awayFinalScoreAvg);
        var homeHalftimeScoreProbability = CalculateScoreProbabilityBy(homeFinalHalftimeScoreAvg);
        var awayHalftimeScoreProbability = CalculateScoreProbabilityBy(awayFinalHalftimeScoreAvg);

        // Average of scoring at least one goal
        var homeCurrentOneGoalAvg = homeCurrentGames
            .Count(i => i.Home == home && i.FullTimeHomeScore > 0 ||
                        i.Away == home && i.FullTimeAwayScore > 0)
            .Divide(homeCurrentGames.Count);
        
        var awayCurrentOneGoalAvg = awayCurrentGames
            .Count(i => i.Home == away && i.FullTimeHomeScore > 0 ||
                        i.Away == away && i.FullTimeAwayScore > 0)
            .Divide(awayCurrentGames.Count);

        var homeLastFourGamesOver = homeCurrentGames.OrderByDescending(i => i.DateTime)
            .Take(4).All(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2);
        
        var awayLastFourGamesOver = awayCurrentGames.OrderByDescending(i => i.DateTime)
            .Take(4).All(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2);

        var homeLastThreeGamesWon = homeCurrentGames.OrderByDescending(i => i.DateTime)
            .Take(3).All(i => i.FullTimeResult == "H" && i.Home == home || i.FullTimeResult == "A" && i.Away == home);
        
        var awayLastThreeGamesWon = awayCurrentGames.OrderByDescending(i => i.DateTime)
            .Take(3).All(i => i.FullTimeResult == "H" && i.Home == away || i.FullTimeResult == "A" && i.Away == away);

        return new TeamAccuracy(
            homeScoreProbability, 
            awayScoreProbability,
            homeHalftimeScoreProbability,
            awayHalftimeScoreProbability,
            homeCurrentOneGoalAvg,
            awayCurrentOneGoalAvg,
            homeShotsAvg,
            awayShotsAvg,
            homeLastFourGamesOver,
            awayLastFourGamesOver,
            homeLastThreeGamesWon,
            awayLastThreeGamesWon
            );
    }

    private static TeamAccuracy AnalyseNeuralNetworkBy(string team, List<Game> games)
    {
        // Team historical games
        var historicalGames = games
            .Where(i => i.Home == team || i.Away == team).ToList();
       
        var neural = new NeuralNetwork(historicalGames);
        neural.TrainAndTestScores();

        return null;
    }

    private static double CalculateScoreProbabilityBy(double average)
    {
        var scoresProb = new List<double>();
        for (var i = 1; i <= 10; i++)
        {
            var prob = CalculatePoissonProbability(average, i);
            scoresProb.Add(prob);
        }

        return scoresProb.Sum();
    }
    
    private static HeadToHead CalculateHeadToHeadBy(string home, string away, List<Game> historicalGames)
    {
        var headToHeadGames = historicalGames
            .Where(i => i.Home == home && i.Away == away || i.Away == home && i.Home == away)
            .OrderByDescending(o => o.DateTime)
            .ToList();

        var bothScoredAvg = headToHeadGames
            .Count(i => i is { FullTimeHomeScore: > 0, FullTimeAwayScore: > 0 })
            .Divide(headToHeadGames.Count); 
        
        var moreThanTwoGoals = headToHeadGames
            .Count(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2)
            .Divide(headToHeadGames.Count); 
        
        var twoToThreeGoals = headToHeadGames
            .Count(i => i.FullTimeHomeScore + i.FullTimeAwayScore == 2 ||
                            i.FullTimeHomeScore + i.FullTimeAwayScore == 3)
            .Divide(headToHeadGames.Count);

       
        return new HeadToHead(headToHeadGames.Count, bothScoredAvg, moreThanTwoGoals, twoToThreeGoals);
    }

    private bool GetLinearClassificationAnalysisBy(string home, string away, List<Game> historicalGames)
{
    // Filter historical games for the given teams
    var games = historicalGames
        .Where(i => (i.Home == home && i.Away == away) || (i.Away == home && i.Home == away))
        .OrderByDescending(o => o.DateTime)
        .ToList();

    // Create MLContext
    var mlContext = new MLContext();

    // Load data into memory
    var data = mlContext.Data.LoadFromEnumerable(games.Select(s => new GameData
    {
        Home = s.Home,
        Away = s.Away,
        HomeScore = s.FullTimeHomeScore ?? 0,
        AwayScore = s.FullTimeAwayScore ?? 0,
        HalftimeHomeGoal = s.HalftimeHomeScore ?? 0,
        HalftimeAwayGoal = s.HalftimeAwayScore ?? 0,
        TotalGoals = s.FullTimeHomeScore + s.FullTimeAwayScore ?? 0,
        Label = s.FullTimeHomeScore + s.FullTimeAwayScore > 2 ? 1 : 0
    }));

    // Split data into training and testing sets
    var trainTestSplit = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);
    var trainData = trainTestSplit.TrainSet;
    var testData = trainTestSplit.TestSet;

    // Define data preparation pipeline
    var dataPipeline = mlContext.Transforms
        .Concatenate("Features", "HomeScore", "AwayScore", "TotalGoals", "HalftimeHomeGoal", "HalftimeAwayGoal") 
        .Append(mlContext.Transforms.NormalizeMinMax("Features"))
        .Append(mlContext.Transforms.Conversion.ConvertType("Label", "Label", DataKind.Boolean));

    // Define trainer algorithm
    var trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression();

    // Define training pipeline
    var trainingPipeline = dataPipeline.Append(trainer);

    // Train the model
    var model = trainingPipeline.Fit(trainData);

    // Evaluate the model
    var predictions = model.Transform(testData);
    var metrics = mlContext.BinaryClassification.Evaluate(predictions);
    Console.WriteLine($"{home}:{away} FastTree Accuracy: {metrics.PositivePrecision:P2}");

    // Test the model with new data
    var currentData = new GameData { Home = home, Away = away };
    var predictionEngine = mlContext.Model.CreatePredictionEngine<GameData, Prediction>(model);
    var prediction = predictionEngine.Predict(currentData);
    var predictedLabel = prediction.Predict;

    return prediction.Predict;
}

   private bool GetFastTreeDecisionBy(string home, string away, List<Game> historicalGames)
{
    // Filter historical games for the given teams
    var games = historicalGames
        .Where(i => i.Home == home || i.Away == home)
        .OrderByDescending(o => o.DateTime)
        .ToList();

    // Create MLContext
    var mlContext = new MLContext();

    // Load data into memory
    var data = mlContext.Data.LoadFromEnumerable(games.Select(s => new GameData
    {
        Home = s.Home,
        Away = s.Away,
        HomeScore = s.FullTimeHomeScore ?? 0,
        AwayScore = s.FullTimeAwayScore ?? 0,
        HalftimeHomeGoal = s.HalftimeHomeScore ?? 0,
        HalftimeAwayGoal = s.HalftimeAwayScore ?? 0,
        TotalGoals = s.FullTimeHomeScore + s.FullTimeAwayScore ?? 0,
        Label = s.FullTimeHomeScore + s.FullTimeAwayScore > 2 ? 1 : 0
    }));

    // Split data into training and testing sets
    var trainTestSplit = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);
    var trainData = trainTestSplit.TrainSet;
    var testData = trainTestSplit.TestSet;

    // Define data preparation pipeline
    var dataPipeline = mlContext.Transforms
        .Concatenate("Features", "HomeScore", "AwayScore", "HalftimeHomeGoal", "HalftimeAwayGoal", "TotalGoals")
        .Append(mlContext.Transforms.NormalizeMinMax("Features"))
        .Append(mlContext.Transforms.Conversion.ConvertType("Label", "Label", DataKind.Boolean));

    // Define trainer algorithm
    var trainer = mlContext.BinaryClassification.Trainers.FastTree();

    // Define training pipeline
    var trainingPipeline = dataPipeline.Append(trainer);

    // Train the model
    var model = trainingPipeline.Fit(trainData);

    // Evaluate the model
    var predictions = model.Transform(testData);
    var metrics = mlContext.BinaryClassification.Evaluate(predictions);
    Console.WriteLine($"{home} FastTree Accuracy: {metrics.Accuracy:P2}");

    // Test the model with new data
    var currentData = new GameData { Home = home, Away = away };
    var predictionEngine = mlContext.Model.CreatePredictionEngine<GameData, Prediction>(model);
    var prediction = predictionEngine.Predict(currentData);
    var predictedLabel = prediction.Predict;

    return predictedLabel;
}
    
    private static double TeamMarkovChainProbability(IEnumerable<Game> pastGames, string team)
    {
        var goalsScored = new Dictionary<int, int>();
        var totalGames = 0;

        foreach (var match in pastGames)
        {
            if (match.Home != team && match.Away != team)
                continue;
            
            var goals = match.Home == team ? match.FullTimeHomeScore ?? 0 : match.FullTimeAwayScore ?? 0;

            if (goalsScored.ContainsKey(goals))
            {
                goalsScored[goals]++;
            }
            else
            {
                goalsScored[goals] = 1;
            }

            totalGames++;
        }

        var goalsScoredSum = goalsScored.Sum(goal => (double)(goal.Value * goal.Key) / totalGames);

        var probabilities = Enumerable.Range(0, 11)
            .Select(score =>
            {
                var probability = CalculatePoissonProbability(goalsScoredSum, score);
                return new MarkovChainResult(KeyBasedOnGoal(score), probability);
            })
            .GroupBy(p => p.Key)
            .Select(g => new MarkovChainResult(
                g.Key, 
                g.Sum(i => i.Probability)))
            .ToList();

        return probabilities.First(i => i.Key == "Score").Probability;
    }
    
    private static string KeyBasedOnGoal(int score)
    {
        var key = score switch
        {
            > 0 => "Score",
            0 => "NoScore",
            _ => throw new ArgumentOutOfRangeException(nameof(score), score, null)
        };
        
        return key;
    }
    
    private static double CalculatePoissonProbability(double lambda, int expectedGoal)
    {
        if (double.IsNaN(lambda) || lambda == 0)
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }

    private (int home, int away) CalculateModeBy(string home, string away, List<Game> games)
    {
        var homeGoal = games.Where(g => g.Home == home || g.Away == home)
            .Select(g => g.Home == home ? g.FullTimeHomeScore ?? 0 : g.FullTimeAwayScore ?? 0)
            .ToArray();

        var awayGoal = games.Where(g => g.Home == away || g.Away == away)
            .Select(g => g.Home == away ? g.FullTimeHomeScore ?? 0 : g.FullTimeAwayScore ?? 0)
            .ToArray();
        
        var homeMode = CalculateMode(homeGoal);
        var awayMode = CalculateMode(awayGoal);

        return (homeMode, awayMode);

    }

    private static int CalculateMode(IEnumerable<int> values)
    {
        var mode = 0;
        var maxCount = 0;
        var valueCounts = new Dictionary<int, int>();

        // Count the frequency of each value
        foreach (var value in values)
        {
            if (valueCounts.ContainsKey(value))
            {
                valueCounts[value]++;
            }
            else
            {
                valueCounts[value] = 1;
            }
        }

        // Find the value with the highest frequency
        foreach (var value in valueCounts.Keys.Where(value => valueCounts[value] > maxCount))
        {
            maxCount = valueCounts[value];
            mode = value;
        }

        return mode;
    }
}

public class GameData
{ 
    [LoadColumn(0)]
    public string Home { get; set; }

    [LoadColumn(1)]
    public string Away { get; set; }

    [LoadColumn(2)]
    public float Label { get; set; }
    
    [LoadColumn(3)]
    public float HomeScore { get; set; }
    [LoadColumn(4)]
    public float AwayScore { get; set; }
    
    [LoadColumn(5)]
    public float TotalGoals { get; set; }
    
    [LoadColumn(6)]
    public float HalftimeHomeGoal { get; set; }
    
    [LoadColumn(7)]
    public float HalftimeAwayGoal { get; set; }
}

public class Prediction
{
    [ColumnName("Label")]
    public bool Predict { get; set; }
}
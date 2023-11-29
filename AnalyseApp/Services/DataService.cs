using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Services;

public class DataService: IDataService
{
    private readonly List<Matches> _historicalMatches;
    private static readonly MLContext mlContext = new MLContext();
    
    public DataService(IFileProcessor fileProcessor)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
    }
    
    public HeadToHeadData GetHeadToHeadDataBy(string homeTeam, string awayTeam, DateTime playedOn)
    {
        var matches = _historicalMatches
            .GetHeadToHeadMatchesBy(homeTeam, awayTeam, playedOn)
            .ToList();

        var homeAtHomeScoreAvg = matches.Where(i => i.HomeTeam == homeTeam).Average(a => a.FTHG).GetValueOrDefault();
        var homeAtAwayScoreAvg = matches.Where(i => i.AwayTeam == homeTeam).Average(a => a.FTAG).GetValueOrDefault();
        var awayAtAwayScoreAvg = matches.Where(i => i.AwayTeam == awayTeam).Average(a => a.FTAG).GetValueOrDefault();
        var awayAtHomeScoreAvg = matches.Where(i => i.HomeTeam == awayTeam).Average(a => a.FTHG).GetValueOrDefault();
        
        var homeProbability = (homeAtHomeScoreAvg * 0.60 + awayAtAwayScoreAvg * 0.40).GetScoredGoalProbabilityBy();
        var awayProbability = (homeAtAwayScoreAvg * 0.40 + awayAtHomeScoreAvg * 0.60).GetScoredGoalProbabilityBy();

        var overTwoGoals = GetOverGameAvg(matches);
        var underThreeGoals = GetUnderGameAvg(matches);
        var twoToThreeGoals = GetTwoToThreeGameAvg(matches);
        var goalGoal = GetBothScoredGameAvg(matches);
        var noGoals = GetZeroScoredGameAvg(matches);
        var overThreeGoals = GetMoreThanThreeGoalGameAvg(matches);
        var homeTeamWon = matches.GetGameAvgBy(
            matches.Count(i => i.HomeTeam == homeTeam),
            match => match.HomeTeam == homeTeam && match.FTHG > match.FTAG ||
                                    match.AwayTeam == homeTeam && match.FTHG < match.FTAG
        );
        var awayTeamWon = matches.GetGameAvgBy(
            matches.Count(i => i.AwayTeam == awayTeam),
            match => match.HomeTeam == awayTeam && match.FTHG > match.FTAG ||
                     match.AwayTeam == awayTeam && match.FTHG < match.FTAG
        );

        var headToHead = new HeadToHeadData(
            matches.Count,
            homeProbability,
            awayProbability,
            overTwoGoals, 
            underThreeGoals, 
            twoToThreeGoals,
            goalGoal,
            noGoals,
            overThreeGoals,
            homeTeamWon,
            awayTeamWon
        );

        headToHead = headToHead with { Suggestion = GetHighValue(headToHeadData: headToHead) };
        
        return headToHead;
    }

    /// <summary>
    /// Prepare Teams data
    /// </summary>
    /// <param name="teamName">Team name</param>
    /// <param name="historicalMatches">List of teams matches</param>
    /// <returns></returns>
    public TeamData GetTeamDataBy(string teamName, IList<Matches> historicalMatches)
    {
        // Query all previous matches of the team
        var games = historicalMatches
            .Where(i => i.HomeTeam == teamName || i.AwayTeam == teamName)
            .ToList();
        
        var goalPower = GetTeamGoalsDataBy(teamName, games);
        var teamResults = GetGameResultAverage(games, teamName);
        var teamOdds = GetMatchOdds(games, teamName);
        
        var lastThreeMatchResult = GetLastThreeMatches(games, teamName);
        var teamScoredGames = GetTeamScoredGamesAvg(games, teamName);
        var teamConcededGames = GetTeamAllowedGamesAvg(games, teamName);
        //
        // ITransformer trainedModel = TrainModel(games);
        //
        // // Example desired averages for prediction
        // float desiredAvgGoalsScored = 2.0f;
        // float desiredAvgGoalsConceded = 1.0f;
        //
        //
        // // Create the TeamPerformance instance for prediction
        // var teamForPrediction = CreateTeamPerformanceForPrediction(desiredAvgGoalsScored, desiredAvgGoalsConceded);
        //
        // var prediction = PredictMatchOutcome(teamForPrediction, trainedModel);
        //
        // var overTwoGoals = prediction.OverTwoGoals;
        
        var teamData = new TeamData(
            games.Count, 
            teamResults,
            teamOdds,
            goalPower,
            teamScoredGames,
            teamConcededGames,
            lastThreeMatchResult
        );

        teamData = teamData with { Suggestion = GetHighValue(teamData: teamData) };
        
        return teamData;
    }

    /// <summary>
    /// Calculate the team's goal average based on whether they are playing at home or away.
    /// </summary>
    /// <param name="teamName">The name of the team for which the goal average is to be calculated.</param>
    /// <param name="matches">A list of match data.</param>
    /// <param name="isHome">A boolean indicating whether to consider home matches (true) or away matches (false).</param>
    /// <returns>TeamGoalAverage object representing the team's recent performance in either home or away games.</returns>
    public TeamGoalAverage CalculateTeamGoalAverageBy(string teamName, IList<Matches> matches, bool isHome = false)
    {
        var homeMatches = matches
            .Where(m => m.Date.Parse() >= new DateTime(2023,07,10))
            .Where(m => m.HomeTeam == teamName)
            .OrderByDescending(i => i.Date.Parse())
            .ToList();
        
        var awayMatches = matches
            .Where(m => m.Date.Parse() >= new DateTime(2023,07,10))
            .Where(m => m.AwayTeam == teamName)
            .OrderByDescending(i => i.Date.Parse())
            .ToList();

        var allMatches = homeMatches.Union(awayMatches).ToList();
        var overallHomeMatches = GetOverallPerformanceBy(allMatches, teamName);
        
        var homeRecentMatches = homeMatches.Take(6).ToList();
        var awayRecentMatches = awayMatches.Take(6).ToList();
        var recentPerformance = GetPerformanceBy(isHome ? homeRecentMatches : awayRecentMatches, isHome);
        
        return new TeamGoalAverage(overallHomeMatches, recentPerformance);
    }

    public HeadToHeadGoalAverage CalculateHeadToHeadAverageBy(string homeTeam, string awayTeam, DateTime playedOn)
    {
        var matches = _historicalMatches
            .GetHeadToHeadMatchesBy(homeTeam, awayTeam, playedOn)
            .ToList();
        
        var homeMatches = matches.Where(m => m.HomeTeam == homeTeam).ToList();
        var awayMatches = matches.Where(m => m.AwayTeam == awayTeam).ToList();
        var head2HeadGoalAverage = GetPerformanceBy(homeMatches, false);

        return new HeadToHeadGoalAverage(head2HeadGoalAverage.ScoredGoalProbability, head2HeadGoalAverage.ConcededGoalProbability);
    }
    
    public List<TeamPerformance> ProcessMatchData(List<Matches> matches)
    {
        var teamPerformances = new Dictionary<string, TeamPerformance>();

        foreach (var match in matches)
        {
            // Update team performance for home team
            if (!teamPerformances.ContainsKey(match.HomeTeam))
                teamPerformances[match.HomeTeam] = new TeamPerformance { TeamName = match.HomeTeam };

            teamPerformances[match.HomeTeam].UpdatePerformance(match.FTHG, match.FTAG);

            // Update team performance for away team
            if (!teamPerformances.ContainsKey(match.AwayTeam))
                teamPerformances[match.AwayTeam] = new TeamPerformance { TeamName = match.AwayTeam };

            teamPerformances[match.AwayTeam].UpdatePerformance(match.FTAG, match.FTHG);
        }

        return teamPerformances.Values.ToList();
    }

    
    private ITransformer TrainModel(List<Matches> historicalMatches)
    {
        var processedData = ProcessMatchData(historicalMatches);
        var dataView = mlContext.Data.LoadFromEnumerable(processedData);

        var dataProcessPipeline = mlContext.Transforms
            .Concatenate(
                "Features",
                nameof(TeamPerformance.AvgGoalsScored),
                nameof(TeamPerformance.AvgGoalsConceded))
            .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "Label"));

        var trainer = mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
            labelColumnName: "Label", featureColumnName: "Features");
        
        var trainingPipeline = dataProcessPipeline.Append(trainer);

        var trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
        var trainedModel = trainingPipeline.Fit(trainTestSplit.TrainSet);
        
        var predictions = trainedModel.Transform(trainTestSplit.TestSet);
        var metrics = mlContext.BinaryClassification.Evaluate(predictions, "Label", "Score");

        // Output the metrics to evaluate the model
        Console.WriteLine($"Accuracy: {metrics.Accuracy}");
        
        return trainedModel;
    }
    
    private MatchOutcome PredictMatchOutcome(TeamPerformance teamPerformance, ITransformer trainedModel)
    {
        var predictionEngine = mlContext.Model.CreatePredictionEngine<TeamPerformance, MatchOutcome>(trainedModel);
        return predictionEngine.Predict(teamPerformance);
    }


    private static GoalPower GetPerformanceBy(IList<Matches> matches, bool isHome)
    {
        var avgGoalsScored = matches.Average(m => isHome ? m.FTHG : m.FTAG).GetValueOrDefault();
        var avgGoalsConceded = matches.Average(m => isHome ? m.FTAG : m.FTHG).GetValueOrDefault();

        return new GoalPower(avgGoalsScored, avgGoalsConceded);
    }
    
    private static GoalPower GetOverallPerformanceBy(ICollection<Matches> matches, string teamName)
    {
        var homeScores = matches.Where(m => m.HomeTeam == teamName).Sum(m => m.FTHG).GetValueOrDefault();
        var awayScores = matches.Where(m => m.AwayTeam == teamName).Sum(m => m.FTAG).GetValueOrDefault();
        
        var homeConceded = matches.Where(m => m.HomeTeam == teamName).Sum(m => m.FTAG).GetValueOrDefault();
        var awayConceded = matches.Where(m => m.AwayTeam == teamName).Sum(m => m.FTHG).GetValueOrDefault();
        
        var avgGoalsScored = (double)(homeScores + awayScores) / matches.Count;
        var avgGoalsConceded = (double)(homeConceded + awayConceded) / matches.Count;

        return new GoalPower(avgGoalsScored, avgGoalsConceded);
    }

    
    private static TeamResult GetGameResultAverage(IReadOnlyCollection<Matches> games, string teamName)
    {
        var lastSixGames = games.Take(6).ToList();
        
        var overTwoGoals = GetOverGameAvg(lastSixGames);
        var overTwoGoalsRecent = GetOverGameAvg(games);
        var finalOverTwoGoals = GetFinalValue(overTwoGoals, overTwoGoalsRecent);
        
        var underTwoGoals = GetUnderGameAvg(games);
        var underTwoGoalsRecent = GetUnderGameAvg(games);
        var finalUnderTwoGoals = GetFinalValue(underTwoGoals, underTwoGoalsRecent);
        
        var twoToThreeGoals = GetTwoToThreeGameAvg(games);
        var twoToThreeGoalsRecent = GetTwoToThreeGameAvg(games);
        var finalTwoToThreeGoals = GetFinalValue(twoToThreeGoals, twoToThreeGoalsRecent);
        
        var bothScoredGoals = GetBothScoredGameAvg(games);
        var bothScoredGoalsRecent = GetBothScoredGameAvg(games);
        var finalBothScoredGoals = GetFinalValue(bothScoredGoals, bothScoredGoalsRecent);
        var noScoredGames = GetNoGoalGameAvg(games, teamName);
        var atLeastOneGoalGameAvg = GetOneOrMoreGoalGameAvg(games, teamName);
        var underFourGoalAvg = GetUnderFourGoalsGameAvg(games, teamName);

        var matchResult = new TeamResult(
            finalOverTwoGoals, 
            finalBothScoredGoals, 
            finalTwoToThreeGoals, 
            finalUnderTwoGoals, 
            noScoredGames,
            atLeastOneGoalGameAvg,
            underFourGoalAvg
        );
        
        return matchResult;
    }

    private static double GetFinalValue(double overTwoGoals, double overTwoGoalsRecent)
    {
        return overTwoGoals * 0.60 + overTwoGoalsRecent * 0.40;
    }

    private static TeamOdds GetMatchOdds(IReadOnlyCollection<Matches> games, string teamName)
    {
        var homeSideWin = GetHomeWinGameAvg(games, teamName);
        var awaySideWin = GetAwayWinGameAvg(games, teamName);
        var draws = GetDrawGameAvg(games);
        var win = (homeSideWin + awaySideWin) / 2;

        var loss = 1 - (draws + win);

        var matchResult = new TeamOdds(homeSideWin, awaySideWin, win, loss, draws);
        return matchResult;
    }

    /// <summary>
    ///  - It calculates average goals scored and conceded both at home and away.
    ///  - It computes the overall and recent scoring and conceding probabilities using a Poisson distribution.
    ///  - The weighted averages is used for recent matches to give more importance to recent performances.
    ///  - The final scoringPower and concededPower are calculated by a weighted sum of overall and recent probabilities.
    /// </summary>
    /// <param name="teamName">Team name</param>
    /// <param name="matches">List of matches</param>
    /// <returns></returns>
    private static GoalPower GetTeamGoalsDataBy(string teamName, IReadOnlyCollection<Matches> matches)
    {
        var homeMatches = matches.Where(item => item.HomeTeam == teamName).ToList();
        var awayMatches = matches.Where(item => item.AwayTeam == teamName).ToList();
        
        var (scoredProbability, concededProbability) = GetProbabilitiesBy(homeMatches, awayMatches);
        
        // Select the last six matches
        homeMatches = homeMatches.Take(6).ToList();
        awayMatches = awayMatches.Take(6).ToList();
        
        var (currentScoredProbability, currentConcededProbability) = GetProbabilitiesBy(homeMatches, awayMatches);;

        // weighted the final scoring and conceding power
        var scoringPower = scoredProbability * 0.35 + currentScoredProbability * 0.65;
        var concededPower = concededProbability * 0.35 + currentConcededProbability * 0.65;
        
        return new GoalPower(scoringPower, concededPower);
    }

    private static (double scoredProbability, double concededProbability) GetProbabilitiesBy(
        IReadOnlyList<Matches> homeMatches, IReadOnlyList<Matches> awayMatches
    )
    {
        // calculate the average scored and conceded goals
        var (homeScoredAvg, homeConcededAvg) = WeightedGoalBy(homeMatches);
        var (awayScoredAvg, awayConcededAvg) = WeightedGoalBy(awayMatches);
        
        // calculate the Poisson probability
        var scoredProbability = (homeScoredAvg + awayScoredAvg).GetScoredGoalProbabilityBy();
        var concededProbability = (homeConcededAvg + awayConcededAvg).GetScoredGoalProbabilityBy();

        return (scoredProbability, concededProbability);
    }

    /// <summary>
    /// - This method calculates weighted averages for goals scored and conceded.
    /// - The weighting strategy (newer games having higher weight) is a logical choice for recent performance analysis.
    /// </summary>
    /// <param name="matches"></param>
    /// <returns></returns>
    private static (double scored, double conceded) WeightedGoalBy(IReadOnlyList<Matches> matches)
    {
        var totalScoredWeight = 0;
        var totalConcededWeight = 0;
        var totalWeight = 0;
        
        for (var i = 0; i < matches.Count; i++)
        {
            // Assigning weights: newer games have higher weight
            var weight = matches.Count - i;

            totalScoredWeight += matches[i].FTHG.GetValueOrDefault() * weight;
            totalConcededWeight += matches[i].FTAG.GetValueOrDefault() * weight;
            totalWeight += weight;
        }
        var weightedScored = totalScoredWeight / (double)totalWeight;
        var weightedConceded = totalConcededWeight / (double)totalWeight;

        return (weightedScored, weightedConceded);
    }
    

    private static LastThreeGameType GetLastThreeMatches(IEnumerable<Matches> matches, string teamName)
    {
        var lastThreeMatches = matches.OrderByDescending(m => m.Date).Take(4).ToList();

        var averages = new List<Average>
        {
            new("Over Two Goals", lastThreeMatches.Count(match => match.FTHG + match.FTAG > 2)),
            new("Under Three Goals", lastThreeMatches.Count(match => match.FTHG + match.FTAG < 3)),
            new("Goal Goal", lastThreeMatches.Count(match => match is { FTHG: > 1, FTAG: > 1 })),
            new("Two to Three Goals", lastThreeMatches.Count(match => match.FTHG + match.FTAG == 2 || match.FTHG + match.FTAG == 3)),
            new("No Goal", lastThreeMatches.Count(match => match.HomeTeam == teamName && match.FTHG == 0 ||
                                                                       match.AwayTeam == teamName && match.FTAG == 0))
        };

        var highestAverage = averages.OrderByDescending(o => o.Count).First();
        var atLeastGoal = new Average("At Least One Goal", lastThreeMatches.Count(match =>
                match.HomeTeam == teamName && match.FTHG > 0 ||
                match.AwayTeam == teamName && match.FTAG > 0));
        
        averages.Add(atLeastGoal);
        
        return new LastThreeGameType(highestAverage, averages);
    }


    private static Suggestion GetHighValue(HeadToHeadData? headToHeadData = null, TeamData? teamData = null)
    {
        Dictionary<string, double> probabilityMap;

        if (teamData is not null)
        {
            probabilityMap = new Dictionary<string, double>
            {
                { "OverScoredGames", teamData.TeamResult.OverTwoGoals },
                { "BothTeamScoredGames", teamData.TeamResult.BothScoredGoals },
                { "TwoToThreeGoalsGames", teamData.TeamResult.TwoToThreeGoals },
                { "UnderScoredGames", teamData.TeamResult.UnderThreeGoals },
            };
        }
        else if (headToHeadData is not null)
        {
            probabilityMap = new Dictionary<string, double>
            {
                { "OverScoredGames", headToHeadData.OverScoredGames },
                { "BothTeamScoredGames", headToHeadData.BothTeamScoredGames },
                { "TwoToThreeGoalsGames", headToHeadData.TwoToThreeGoalsGames },
                { "UnderScoredGames", headToHeadData.UnderThreeScoredGames }
            };
        }
        else
        {
            throw new ArgumentException("Either HeadToHeadData or TeamData must be provided.");
        }

        var result = probabilityMap.MaxBy(i => i.Value);
        
        return new Suggestion(result.Key, result.Value);
    }

    private static double GetTeamScoredGooalsAvg(IReadOnlyCollection<Matches> matches, string teamName)
    {
        var goals = matches.Where(item => item.HomeTeam == teamName).Sum(s => s.FTHG) +
                            matches.Where(item => item.AwayTeam == teamName).Sum(s => s.FTAG);

        var avg = goals / (double)matches.Count;

        return avg.GetValueOrDefault();
    }
    
    private static double GetTeamScoredGamesAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(matches.Count, match => match.FTHG > 0 && match.HomeTeam == teamName ||
                                                     match.FTAG > 0 && match.AwayTeam == teamName);
    
    private static double GetTeamAllowedGamesAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(matches.Count, match => match.FTAG > 0 && match.HomeTeam == teamName ||
                                                     match.FTHG > 0 && match.AwayTeam == teamName);

    private static double GetTwoToThreeGameAvg(IReadOnlyCollection<Matches> matches) => 
        matches.GetGameAvgBy(
            matches.Count, 
            match => match.FTHG + match.FTAG == 3 || match.FTHG + match.FTAG == 2
        );

    private static double GetMoreThanThreeGoalGameAvg(IReadOnlyCollection<Matches> matches) => 
        matches.GetGameAvgBy(matches.Count, match => match.FTHG + match.FTAG > 3);
    
    private static double GetUnderGameAvg(IReadOnlyCollection<Matches> matches) => 
        matches.GetGameAvgBy(matches.Count, match => match.FTHG + match.FTAG < 2);

    private static double GetOverGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match.FTHG + match.FTAG > 2);
    
    private static double GetBothScoredGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match is { FTHG: > 0, FTAG: > 0 });
    
    private static double GetZeroScoredGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match is { FTHG: 0, FTAG: 0 });
    
    
    private static double GetNoGoalGameAvg(IReadOnlyCollection<Matches> matches, string teamName)
    { 
        var homeGames = matches.Where(match => match.HomeTeam == teamName).ToList();
        var awayGames = matches.Where(match => match.AwayTeam == teamName).ToList();

        // Check to avoid division by zero
        var homeNoGoals = homeGames.Any() ? homeGames.Count(match => match.FTHG == 0) / (double)homeGames.Count : 0;
        var awayNoGoals = awayGames.Any() ? awayGames.Count(match => match.FTHG == 0) / (double)awayGames.Count : 0;

        // Correctly calculating the average
        var finalAverage = (homeNoGoals + awayNoGoals) / 2;

        return finalAverage;
    }

    private static double GetOneOrMoreGoalGameAvg(IReadOnlyCollection<Matches> matches, string teamName)
    { 
        var homeGames = matches
            .Where(match => match.HomeTeam == teamName)
            .ToList();

        // Calculate proportion of home games where the team scored one or more goals
        var homeOneOrMoreGoals = homeGames.Any() ? homeGames.Count(match => match.FTHG > 1) / (double)homeGames.Count : 0;
    
        var awayGames = matches
            .Where(match => match.AwayTeam == teamName)
            .ToList();
    
        // Calculate proportion of away games where the team scored one or more goals
        var awayOneOrMoreGoals = awayGames.Any() ? awayGames.Count(match => match.FTAG > 1) / (double)awayGames.Count : 0;

        // Calculate the average
        var finalAverage = (homeOneOrMoreGoals + awayOneOrMoreGoals) / 2;

        return finalAverage;
    }
    
    private static double GetMoreThenTwoGoalGameAvg(IReadOnlyCollection<Matches> matches, string teamName)
    { 
        var overTwoGoalGamesAvg = matches.Any() 
            ? matches.Count(match => match.FTAG + match.FTHG > 2) / (double)matches.Count : 0;

        return overTwoGoalGamesAvg;
    }
    
    
    private static double GetUnderFourGoalsGameAvg(IReadOnlyCollection<Matches> matches, string teamName)
    { 
        var overTwoGoalGamesAvg = matches.Any() 
            ? matches.Count(match => match.FTAG + match.FTHG < 4) / (double)matches.Count : 0;

        return overTwoGoalGamesAvg;
    }

    
    private static double GetHomeWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(
            matches.Count(i => i.HomeTeam == teamName),
            match => match.HomeTeam == teamName && match.FTHG > match.FTAG);
   
    private static double GetDrawGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match.FTHG == match.FTAG);

    private static double GetAwayWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(
            matches.Count(i => i.AwayTeam == teamName), 
            match => match.AwayTeam == teamName && match.FTHG < match.FTAG);
    
    private static double GetWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(
            matches.Count, 
            match => (match.AwayTeam == teamName && match.FTHG < match.FTAG) ||
                                    match.HomeTeam == teamName && match.FTHG > match.FTAG
        );
}
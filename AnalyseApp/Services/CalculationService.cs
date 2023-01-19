using AnalyseApp.Extensions;
using AnalyseApp.models;
using MathNet.Numerics.Distributions;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;

namespace AnalyseApp.Services;

public class CalculationService
{
    private readonly List<GameData> _gameData;
    private readonly MLContext _mlContext;
    private readonly List<GameData> _upComingMatch;
    
    public CalculationService(List<GameData> gameData, List<GameData> upComingMatch)
    {
        _gameData = gameData;
        _mlContext = new MLContext();
        _upComingMatch = upComingMatch;
    }

    public List<MatchProbability> Execute(string homeTeam, string awayTeam, string league)
    {
        var result = new List<MatchProbability>();
        var lastSeasons = AnalyseTheSeasonPerformanceBy(homeTeam, awayTeam, league, 2018, 2022);
        var currentSeason = AnalyseTheSeasonPerformanceBy(homeTeam, awayTeam, league, 2022, 2023); 
        var currentGameBookmakers = GetBet365BookmakersValuesBy(homeTeam, awayTeam);

        /*
         var homeBet365 = 1 / 2.0;
         var awayBet365 = 1 / 3.60;
         var drawBet365 = 1 / 3.40;
         var goalGoalBet365 = 1 / 1.90;
         var moreThanTwoGoalBet365 = 1 / 2.10;
         var TwoToThreeGoalBet365 = 1 / 2.05;
         var lessThanThreeGoalBet365 = 1 / 1.72;

        
        var homeBet365 = 1 / 2.45;
        var awayBet365 = 1 / 2.90;
        var drawBet365 = 1 / 3.80;
        var goalGoalBet365 = 1 / 2.00;
        var moreThanTwoGoalBet365 = 1 / 2.37;
        var TwoToThreeGoalBet365 = 1 / 2.00;
        var lessThanThreeGoalBet365 = 1 / 1.57;
        */
        
        var homeBet365 = 1 / 1.40;
        var awayBet365 = 1 / 7.50;
        var drawBet365 = 1 / 5.00;
        var goalGoalBet365 = 1 / 1.80;
        var moreThanTwoGoalBet365 = 1 / 1.61;
        var TwoToThreeGoalBet365 = 1 / 2.00;
        var lessThanThreeGoalBet365 = 1 / 2.30;
        var home = CalculateWeighting(lastSeasons["HomeWin"], currentSeason["HomeWin"]);
        var away = CalculateWeighting(lastSeasons["AwayWin"], currentSeason["AwayWin"]);
        var draw = CalculateWeighting(lastSeasons["Draw"], currentSeason["Draw"]);
        var goalGoal = CalculateWeighting(lastSeasons["BothTeamScore"], currentSeason["BothTeamScore"]);
        var moreThanTwoGoals = CalculateWeighting(lastSeasons["MoreThanTwoGoals"], currentSeason["MoreThanTwoGoals"]);
        var twoToThreeGoal = CalculateWeighting(lastSeasons["TwoToThree"], currentSeason["TwoToThree"]);
        var zeroZeroGoal = CalculateWeighting(lastSeasons["ZeroZeroGoal"], currentSeason["ZeroZeroGoal"]);
        var lessThanThreeGoal = CalculateWeighting(lastSeasons["LessThanTwoGoals"], currentSeason["LessThanTwoGoals"]);
        
        foreach (var allSeason in lastSeasons)
        {
            var currentSeasonProbability = currentSeason
                .Where(i => i.Key == allSeason.Key)
                .Select(ii => ii.Value)
                .FirstOrDefault();

           
              
            result.Add(new MatchProbability
            {
                Key = allSeason.Key,
                Probability = allSeason.Value.CalculateWeighting(currentSeasonProbability),
                Bet365BookMaker = 1 / GetBet365BookMakersProbabilityBy(currentGameBookmakers, allSeason.Key)
            });
            
        }

        return result;
        //var model = Train(homeTeam, awayTeam);


        //PredictMatch(model);
        //  var homeWin = lastSeason.Values + currentSeason.Values;
        //  var awayWin = awayWinMatches.Sum(i => i.Value[1]);
        //  var draw = drawMatches.Sum(i => i.Value[0]);


    }
    
    private double GetBet365BookMakersProbabilityBy(GameData gameData, string key)
    {
        return key switch
        {
            nameof(gameData.BothTeamScore) => Convert.ToDouble(gameData.BothTeamScore),
            nameof(gameData.MoreThanTwoGoals) => Convert.ToDouble(gameData.MoreThanTwoGoals),
            nameof(gameData.LessThanTwoGoals) => Convert.ToDouble(gameData.LessThanTwoGoals),
            nameof(gameData.TwoToThree) => Convert.ToDouble(gameData.TwoToThree),
            nameof(gameData.Draw) => Convert.ToDouble(gameData.Draw),
            nameof(gameData.AwayWin) => Convert.ToDouble(gameData.AwayWin),
            nameof(gameData.HomeWin) => Convert.ToDouble(gameData.HomeWin),
            _ => 0
        };
    }
    
    private GameData GetBet365BookmakersValuesBy(string homeTeam, string awayTeam)
    {
        var match = _upComingMatch
            .First(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam);
        
        return match;
    }

    private static double CalculateWeighting(double left, double right)
    {
        var result = left * 0.35 + right * 0.65;
        return Math.Round(result, 2);
    }


    private TransformerChain<NormalizingTransformer> Train(string homeTeam, string awayTeam)
    {
        var data = _gameData
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == awayTeam)
            .Select(i => new FootballData
            {
                AwayTeamScore = Convert.ToSingle(i.FTAG),
                HomeTeamScore = Convert.ToSingle(i.FTHG),
            })
            .ToList();
        
        var dataView = _mlContext.Data.LoadFromEnumerable(data);

        var pipeline = _mlContext.Transforms.Concatenate(
                "Features", "HomeTeamScore", "AwayTeamScore")
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"));

        var pipeline2 =
            _mlContext.Transforms.Concatenate("Features", "HomeTeamScore", "AwayTeamScore")
            .Append(_mlContext.Regression.Trainers.FastTree());
        /*
        var model2 = pipeline2.Fit(dataView);
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<FootballData, MatchPrediction>(model2);
        var matchData = new FootballData { HomeTeamScore = 2, AwayTeamScore = 0 };
        var probability = predictionEngine.Predict(matchData);
        Console.WriteLine($"Predicted score: {probability.Score}");
        
        */
        
        var model = pipeline.Fit(dataView);

        return model;
    }

    private void PredictMatch(ITransformer model)
    {
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<FootballData, MatchPrediction>(model);
        for (var homeScore = 0; homeScore <= 6; homeScore++)
        {
            for (var awayScore = 0; awayScore <= homeScore; awayScore++)
            {
                var matchData = new FootballData { HomeTeamScore = homeScore, AwayTeamScore = awayScore };
                var probability = predictionEngine.Predict(matchData);
                Console.WriteLine($"Predicted score: {homeScore}:{awayScore} Probability: {probability.Score}");
            }
        }
    }
    
    private Dictionary<string, double> AnalyseTheSeasonPerformanceBy(
        string homeTeam, string awayTeam, string league, int startYear, int endYear)
    {
        // Retrieving the season of the league by year.
        var leagueSeason = GetLeagueSeasonBy(startYear, endYear, league);
        
        // Checking if the given home team and away team are part of the league.
        var teams = leagueSeason.Select(i => i.HomeTeam)
            .Distinct()
            .ToList();

        if (teams.All(i => i != homeTeam)||teams.All(i => i != awayTeam))
            return null;
        
        // Compute the average goals scored and conceded for all games in the season of the given league at home
        var (averageAllHomeScored, averageAllHomeConcededScored) = CalcScoredAndConcededScoredAverage(
            leagueSeason, true);
        
        // Compute the average goals scored and conceded for all games in the season of the given league on away field
        var (averageAllAwayScored, averageAllAwayConcededScored) = CalcScoredAndConcededScoredAverage(
            leagueSeason, default);


        var home = leagueSeason
            .Where(i => i.HomeTeam == homeTeam)
            .ToList();
        
        var away = leagueSeason
            .Where(i => i.AwayTeam == awayTeam)
            .ToList();

        // Compute the average goals scored and conceded for each team in specific field
        var (averageHomeScored, averageHomeConcededScored) = CalcScoredAndConcededScoredAverage(home, true);
        var (averageAwayScored, averageAwayConcededScored) = CalcScoredAndConcededScoredAverage(away, default);
         
        // Calculates the performance of each team based on their average goals scored and conceded compared to the league averages.
        var homeScoredPerformance = CalculatePerformance(averageHomeScored, averageAllHomeScored);
        var awayScoredPerformance = CalculatePerformance(averageAwayScored, averageAllAwayScored);
        var homeScoredConcededPerformance = CalculatePerformance(averageHomeConcededScored, averageAllHomeConcededScored);
        var awayScoredConcededPerformance = CalculatePerformance(averageAwayConcededScored, averageAllAwayConcededScored);

       var expectedHomeGoal = homeScoredPerformance * awayScoredConcededPerformance * averageAllHomeScored;
       var expectedAwayGoal = awayScoredPerformance * homeScoredConcededPerformance * averageAllAwayScored;
      // var expectedHomeGoal = homeScoredPerformance + awayScoredConcededPerformance / 2;
      // var expectedAwayGoal = awayScoredPerformance + homeScoredConcededPerformance / 2;
       var result = Probability(expectedHomeGoal, expectedAwayGoal);

        return result;
    }

    private static double CalculatePerformance(double left, double right) => left / right;
    
    private static (double homeScored, double homeConcededScored) CalcScoredAndConcededScoredAverage(
        ICollection<GameData> gameData, bool isHome)
    {
        var scored = gameData.Count(i => isHome ? i.FTHG > 0 : i.FTAG > 0);
        var concededScored = gameData.Count(i => isHome ? i.FTAG > 0 : i.FTHG > 0);
        
        var averageScored = scored / (double)gameData.Count;
        var averageConcededScored = concededScored / (double)gameData.Count;
        
        var result = (averageScored, averageConcededScored);
        
        return result;
    }
    
    private Dictionary<string, double> Probability(double homeAverage, double awayAverage)
    {
        var homeWinMatches = new Dictionary<int[], double>();
        var goalGoalScore = new Dictionary<int[], double>();
        var moreThanTwoScore = new Dictionary<int[], double>();
        var twoToThreeScore = new Dictionary<int[], double>();
        var zeroZero = new Dictionary<int[], double>();
        var lessThanThreeGoals = new Dictionary<int[], double>();
        var awayWinMatches = new Dictionary<int[], double>();
        var drawMatches = new Dictionary<int[], double>();
        
        for (var homeScore = 0; homeScore <= 10; homeScore++)
        {
            for (var awayScore = 0; awayScore <= 10; awayScore++)
            {
                var homePoissonProbability = CalculatePoissonProbability(homeAverage, homeScore);
                var awayPoissonProbability = CalculatePoissonProbability(awayAverage, awayScore);

                var probability = homePoissonProbability * awayPoissonProbability;

                if (homeScore > awayScore)
                {
                    homeWinMatches.Add(new []{ homeScore, awayScore }, probability);
                }
                else if (homeScore < awayScore)
                {
                    awayWinMatches.Add(new []{ homeScore, awayScore }, probability);
                }
                else
                {
                    drawMatches.Add(new []{ homeScore, awayScore }, probability);
                }
                if (homeScore > 0 && awayScore > 0)
                {
                    goalGoalScore.Add(new []{ homeScore, awayScore }, probability);
                }

                if (homeScore + awayScore > 2)
                {
                    moreThanTwoScore.Add(new []{ homeScore, awayScore }, probability);
                }
                if (homeScore + awayScore <= 3 && homeScore + awayScore > 1)
                {
                    twoToThreeScore.Add(new []{ homeScore, awayScore }, probability);
                }
                if (homeScore + awayScore < 3)
                {
                    lessThanThreeGoals.Add(new []{ homeScore, awayScore }, probability);
                }
                if (homeScore is 0 && awayScore is 0)
                {
                    zeroZero.Add(new []{ homeScore, awayScore }, probability);
                }
            }
        }

        var home = homeWinMatches.Sum(i => i.Value);
        var away = awayWinMatches.Sum(i => i.Value);
        var matchDraw = drawMatches.Sum(i => i.Value);
        var goalGoal = goalGoalScore.Sum(i => i.Value);
        var moreThanTwoGoal = moreThanTwoScore.Sum(i => i.Value);
        var twoToThreeGoal = twoToThreeScore.Sum(i => i.Value);
        var zeroZeroGoal = zeroZero.Sum(i => i.Value);
        var lessThanThree = lessThanThreeGoals.Sum(i => i.Value);
        return new Dictionary<string, double>
        {
            {"HomeWin", home},
            {"AwayWin", away},
            {"Draw", matchDraw},
            {"BothTeamScore", goalGoal},
            {"MoreThanTwoGoals", moreThanTwoGoal},
            {"TwoToThree", twoToThreeGoal},
            {"ZeroZeroGoal", zeroZeroGoal},
            {"LessThanTwoGoals", lessThanThree},
            
            
        };
    }
    
    private static double CalculatePoissonProbability(double lambda, int expectedGoal)
    {
        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }
    
    private IList<GameData> GetLeagueSeasonBy(int startYear, int endYear, string league)
    {
        var startDate = new DateTime(startYear, 08, 01);
        var endDate = new DateTime(endYear, 06, 30);

        var filteredMatches = _gameData.Where(i => 
        {
            var matchDate = DateTime.Parse(i.Date);
            return matchDate >= startDate && matchDate <= endDate && i.Div == league;
        }).ToList();

        return filteredMatches;
    }
}
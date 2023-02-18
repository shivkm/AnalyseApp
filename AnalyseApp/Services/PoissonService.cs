using AnalyseApp.Extensions;
using AnalyseApp.Models;
using MathNet.Numerics.Distributions;

namespace AnalyseApp.Services;

public interface IPoissonService
{
    public List<PoissonProbability> Execute(
        string homeTeam, string awayTeam, string league);
}

public class PoissonService : IPoissonService
{
        private readonly IList<HistoricalGame> _currentSeason;
    private readonly IList<HistoricalGame> _lastSixSeason;
    private readonly Dictionary<string, Dictionary<int[], double>> _poissonProbabilityDictionary = new ();
    
    public PoissonService(IReadOnlyCollection<HistoricalGame> gameData)
    {
        _currentSeason = gameData.GetGameDataBy(2022, 2023);
        _lastSixSeason = gameData.GetGameDataBy(2016, 2022);
    }

    public List<PoissonProbability> Execute(string homeTeam, string awayTeam, string league)
    {
        var currentSeason = AnalysePerformance(homeTeam, awayTeam, league, _currentSeason);
        var allSeasons = AnalysePerformance(homeTeam, awayTeam, league, _lastSixSeason);

        return (from allSeason in allSeasons
            let currentSeasonProbability = currentSeason
                .Where(i => i.Key == allSeason.Key)
                .Select(ii => ii.Value)
                .FirstOrDefault()
            select new PoissonProbability
            {
                Key = allSeason.Key, 
                Probability = allSeason.Value.CalculateWeighting(currentSeasonProbability)
            }).ToList();
    }
 
    private Dictionary<string, double> AnalysePerformance(string homeTeam, string awayTeam, string league,  IList<HistoricalGame> historicalData)
    {
        // Retrieving the season of the league by year.
        if (!historicalData.TeamsAreInLeague(homeTeam, awayTeam))
            return new Dictionary<string, double>();
        
        var homeMatches = CalculateTeamStrengthBy(historicalData, homeTeam, league, true);
        var awayMatches = CalculateTeamStrengthBy(historicalData, awayTeam, league);
            
        var expectedHomeGoal = homeMatches.Attack * awayMatches.Defense * homeMatches.LeagueScoredAverage;
        var expectedAwayGoal = awayMatches.Attack * homeMatches.Defense * homeMatches.LeagueConcededAverage;
        var probabilities = PossibleProbabilities(expectedHomeGoal, expectedAwayGoal);
        
        _poissonProbabilityDictionary.Clear();
        return probabilities;
    }
    
    private Dictionary<string, double> PossibleProbabilities(double homeGoalAverage, double awayGoalAverage)
    {
        var output = new Dictionary<string, double>();
        for (var homeScore = 0; homeScore <= 10; homeScore++)
        {
            for (var awayScore = 0; awayScore <= 10; awayScore++)
            {
                var homePoissonProbability = CalculatePoissonProbability(homeGoalAverage, homeScore);
                var awayPoissonProbability = CalculatePoissonProbability(awayGoalAverage, awayScore);
                
                AddScoreProbabilities(homeScore, awayScore, homePoissonProbability * awayPoissonProbability);
              //  AddOddProbabilities(homeScore, awayScore,homePoissonProbability * awayPoissonProbability);

            }
        }
        
        foreach (var item in _poissonProbabilityDictionary)
        {
            var sumUpValue = item.Value.Sum(i => i.Value);
            output.Add(item.Key, sumUpValue);
        }
        return output;
    }

    private void AddScoreProbabilities(int homeScore, int awayScore, double probability)
    {
        var result = new Dictionary<int[], double>();
        if (homeScore + awayScore > 2)
        {
            AddOrUpdateDictionary(homeScore, awayScore, probability, "MoreThanTwoGoals", result);
        }
        if (homeScore > 0 && awayScore > 0)
        {
            AddOrUpdateDictionary(homeScore, awayScore, probability, "BothTeamScore", result);
        }
        if (homeScore + awayScore == 3 || homeScore + awayScore == 2)
        {
            AddOrUpdateDictionary(homeScore, awayScore, probability, "TwoToThree", result);
        }
        if (homeScore is 0 && awayScore is 0)
        {
            AddOrUpdateDictionary(homeScore, awayScore, probability, "ZeroZeroGoals", result);
        }
        if (homeScore is 1 or 2 && awayScore is 0 || awayScore is 1 or 2 && homeScore is 0)
        {
            AddOrUpdateDictionary(homeScore, awayScore, probability, "OneSideGoal", result);
        }
        if (homeScore + awayScore < 3)
        {
            AddOrUpdateDictionary(homeScore, awayScore, probability, "LessThanTwoGoals", result);
        }
    }

    private void AddOrUpdateDictionary(
        int homeScore, int awayScore, double probability, string dictKey, Dictionary<int[], double> result)
    {
        var dictionaryKey = new[] { homeScore, awayScore };
        result.Add(dictionaryKey, probability);
        
        if (_poissonProbabilityDictionary.ContainsKey(dictKey))
        {
            if (!_poissonProbabilityDictionary[dictKey].ContainsKey(dictionaryKey))
                _poissonProbabilityDictionary[dictKey].Add(dictionaryKey, probability);
        }
        else
        {
            var innerDictionary = new Dictionary<int[], double>
            {
                { dictionaryKey, probability }
            };
            _poissonProbabilityDictionary.Add(dictKey, innerDictionary);
        }
    }

    private void AddOddProbabilities(int homeScore, int awayScore, double probability)
    {
        var result = new Dictionary<int[], double>();
        if (homeScore == awayScore)
        {
            AddOrUpdateDictionary(homeScore, awayScore, probability, "Draw", result);
        }
        if (homeScore > awayScore)
        {
            AddOrUpdateDictionary(homeScore, awayScore, probability, "HomeWin", result);
        }
        if (homeScore < awayScore)
        {
            AddOrUpdateDictionary(homeScore, awayScore, probability, "AwayWin", result);
        }
    }

    private static double CalculatePoissonProbability(double lambda, int expectedGoal)
    {
        if (double.IsNaN(lambda) || lambda == 0)
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }
    
    private static TeamStrength CalculateTeamStrengthBy(IList<HistoricalGame> gameData, string team, string league, bool atHome = false)
    {
        var currentTeamGames = gameData
            .Where(i => atHome ? i.HomeTeam == team : i.AwayTeam == team)
            .ToList();

        var leagueTeamGames = gameData
            .Where(i => i.Div == league)
            .ToList();
        
        var leagueGoalAverage = CalculateGoalAverage(leagueTeamGames, currentTeamGames.Count, atHome);
        var teamGoalAverage =  CalculateGoalAverage(currentTeamGames.ToList(), atHome: atHome);

        var attack = teamGoalAverage.Scored.Divide(leagueGoalAverage.Scored);
        var defense = teamGoalAverage.Conceded.Divide(leagueGoalAverage.Conceded);

        return new TeamStrength(
            attack, 
            defense, 
            teamGoalAverage.Scored,
            teamGoalAverage.Conceded,
            leagueGoalAverage.Scored, 
            leagueGoalAverage.Conceded);
    }
    
    
    /// <summary>
    /// Compute the average goal scored and conceded for given team.
    /// </summary>
    /// <param name="gameData">List of the past games</param>
    /// <param name="count">provide if the League score</param>
    /// <param name="atHome">provide if the League score</param>
    /// <returns></returns>
    private static GoalScoredAndConcededAverage CalculateGoalAverage(IList<HistoricalGame> gameData, int count = 0, bool atHome = false)
    {
        var averageScored = gameData.GetGoalScoreAverage(count, atHome: atHome);
        var averageConcededScored = gameData.GetGoalConcededAverage(count, atHome: atHome);
        
        var result = new GoalScoredAndConcededAverage(averageScored, averageConcededScored);
        
        return result;
    }
    /*
    private readonly IList<HistoricalGame> _currentSeason;
    private readonly IList<HistoricalGame> _lastSixSeason;
    private readonly Dictionary<string, Dictionary<int[], double>> _poissonProbabilityDictionary = new ();
    
    public PoissonService(IReadOnlyCollection<HistoricalGame> gameData)
    {
        _currentSeason = gameData.GetGameDataBy(2022, 2023);
        _lastSixSeason = gameData.GetGameDataBy(2016, 2022);
    }

    public List<PoissonProbability> Execute(string homeTeam, string awayTeam, string league)
    {
        var currentPossibleProbabilities = PossibleProbability(homeTeam, awayTeam, league, _currentSeason);
        var possibleProbabilities = PossibleProbability(homeTeam, awayTeam, league, _lastSixSeason);
        var result = new List<PoissonProbability>();
        
        foreach (var currentProbability in currentPossibleProbabilities)
        {
            var allPoisonProbability = possibleProbabilities
                .Where(i => i.Key == currentProbability.Key)
                .Select(ii => ii.PoisonProbability)
                .FirstOrDefault();

            result.Add(new PoissonProbability
            {
                Key = currentProbability.Key, 
                Probability = allPoisonProbability.CalculateWeighting(currentProbability.PoisonProbability)
            });
        }

        return result;
    }
 
    private static List<TeamProbability> PossibleProbability(
        string homeTeam, string awayTeam, string league,  IList<HistoricalGame> historicalData)
    {
        if (!historicalData.TeamsAreInLeague(homeTeam, awayTeam))
            return new List<TeamProbability>();
        
        var homeMatches = CalculateTeamStrengthBy(historicalData, homeTeam, league, true);
        var awayMatches = CalculateTeamStrengthBy(historicalData, awayTeam, league);
        
        var expectedHomeGoal = homeMatches.Attack * awayMatches.Defense * homeMatches.LeagueScoredAverage;
        var expectedAwayGoal = awayMatches.Attack * homeMatches.Defense * homeMatches.LeagueConcededAverage;
        var probabilities = PoisonPossibleProbability(expectedHomeGoal, expectedAwayGoal);

        return probabilities;
    }

    /// <summary>
    /// Calculate the team strength for poison probability
    /// </summary>
    /// <param name="gameData"></param>
    /// <param name="team"></param>
    /// <param name="league"></param>
    /// <param name="atHome"></param>
    /// <returns></returns>
    private static TeamStrength CalculateTeamStrengthBy(IList<HistoricalGame> gameData, string team, string league, bool atHome = false)
    {
        var currentTeamGames = gameData
            .Where(i => atHome ? i.HomeTeam == team : i.AwayTeam == team)
            .ToList();

        var leagueTeamGames = gameData
            .Where(i => i.Div == league)
            .ToList();
        
        var leagueGoalAverage = CalculateGoalAverage(leagueTeamGames, currentTeamGames.Count, atHome);
        var teamGoalAverage =  CalculateGoalAverage(currentTeamGames.ToList(), atHome: atHome);

        var attack = teamGoalAverage.Scored.Divide(leagueGoalAverage.Scored);
        var defense = teamGoalAverage.Conceded.Divide(leagueGoalAverage.Conceded);

        return new TeamStrength(
            attack, 
            defense, 
            teamGoalAverage.Scored,
            teamGoalAverage.Conceded,
            leagueGoalAverage.Scored, 
            leagueGoalAverage.Conceded);
    }
    
    
    /// <summary>
    /// Compute the average goal scored and conceded for given team.
    /// </summary>
    /// <param name="gameData">List of the past games</param>
    /// <param name="count">provide if the League score</param>
    /// <param name="atHome">provide if the League score</param>
    /// <returns></returns>
    private static GoalScoredAndConcededAverage CalculateGoalAverage(
        IList<HistoricalGame> gameData, int count = 0, bool atHome = false)
    {
        var averageScored = gameData.GetGoalScoreAverage(count, atHome: atHome);
        var averageConcededScored = gameData.GetGoalConcededAverage(count, atHome: atHome);
        
        var result = new GoalScoredAndConcededAverage(averageScored, averageConcededScored);
        
        return result;
    }
    
    private static List<TeamProbability> PoisonPossibleProbability(double homeGoalAverage, double awayGoalAverage)
    {
        var probabilities = new List<TeamProbability>();

        for (var homeScore = 0; homeScore <= 10; homeScore++)
        {
            for (var awayScore = 0; awayScore <= 10; awayScore++)
            {
                var homePoissonProbability = Poisson(homeGoalAverage, homeScore);
                var awayPoissonProbability = Poisson(awayGoalAverage, awayScore);

                var key = "";
                if (homeScore + awayScore > 2)
                {
                    key = "MoreThanTwoScore";
                }
                if (homeScore > 0 && awayScore > 0)
                {
                    key = "BothScored";
                }
                if (homeScore + awayScore == 3 ||
                         homeScore + awayScore == 2)
                {
                    key = "TwoToThreeScored";
                }
                if (homeScore == 0 && awayScore == 0)
                {
                    key = "ZeroZeroScored";
                }

                if (key != "")
                {
                    probabilities.Add(new TeamProbability
                    {
                        Key = key,
                        PoisonProbability = homePoissonProbability * awayPoissonProbability
                    });
                }
            }
        }

        var result = probabilities
            .GroupBy(p => p.Key)
            .Select(g => new TeamProbability
            {
                Key = g.Key,
                PoisonProbability = g.Sum(i => i.PoisonProbability)
            })
            .ToList();

        return result;
    }
   
    private static double Poisson(double lambda, int expectedGoal)
    {
        if (double.IsNaN(lambda) || lambda == 0)
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }
*/
 }
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
        var currentSeason = AnalysePerformance(homeTeam, awayTeam, _currentSeason);
        var allSeasons = AnalysePerformance(homeTeam, awayTeam, _lastSixSeason);

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
 
    private Dictionary<string, double> AnalysePerformance(
        string homeTeam, string awayTeam, IList<HistoricalGame> historicalData)
    {
        // Retrieving the season of the league by year.
        
        if (!historicalData.TeamsAreInLeague(homeTeam, awayTeam))
            return new Dictionary<string, double>();
        
        var homeMatches = CalculateTeamStrengthBy(historicalData, homeTeam, true);
        var awayMatches = CalculateTeamStrengthBy(historicalData, awayTeam);
            
        var expectedHomeGoal = homeMatches.Attack * awayMatches.Defense * homeMatches.LeagueScored;
        var expectedAwayGoal = awayMatches.Attack * homeMatches.Defense * homeMatches.LeagueConceded;
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
    
    private static TeamStrength CalculateTeamStrengthBy(IList<HistoricalGame> gameData,
        string team, bool atHome = false)
    {
        var currentTeamGames = gameData.GetTeamMatchesBy(team, atHome);
        var leagueGames = CalculateGoalAverage(gameData, currentTeamGames.Count, atHome);
        var currentGames =  CalculateGoalAverage(currentTeamGames, isHome: atHome);

        var attack = currentGames.Scored.Divide(leagueGames.Scored);
        var defense = currentGames.Conceded.Divide(leagueGames.Conceded);

        return new TeamStrength(attack, defense, leagueGames.Scored, leagueGames.Conceded);
    }
    
    // Compute the average goals scored and conceded for all games in the season of the given league at home
    private static PoissonAverage CalculateGoalAverage(
        ICollection<HistoricalGame> gameData, int count = 0, bool isHome = false, bool halftime = false)
    {
        var scored = GetSumOfScoredGoals(gameData, isHome, halftime);
        var concededScored = GetSumOfConcededGoals(gameData, isHome, halftime);
        var countValue = (double)(count > 0 ? count * gameData.NumberOfTeamsLeague(): gameData.Count);

        var averageScored = scored.Divide(countValue);
        var averageConcededScored = concededScored.Divide(countValue);
        
        var result = new PoissonAverage(averageScored, averageConcededScored);
        
        return result;
    }

    private static double GetSumOfScoredGoals(IEnumerable<HistoricalGame> gameData, bool isHome = false, bool halftime = false)
    {
        if (isHome)
            return halftime ? gameData.Sum(i => i.HTHG ?? 0) : gameData.Sum(i => i.FTHG ?? 0);

        return halftime ? gameData.Sum(i => i.HTAG ?? 0) : gameData.Sum(i => i.FTAG ?? 0);
    }
    
    private static double GetSumOfConcededGoals(IEnumerable<HistoricalGame> gameData, bool isHome = false, bool halftime = false)
    {
        if (isHome)
            return halftime ? gameData.Sum(i => i.HTAG ?? 0) : gameData.Sum(i => i.FTAG ?? 0);
        
        return halftime ? gameData.Sum(i => i.HTHG ?? 0) : gameData.Sum(i => i.FTHG ?? 0);
    }
 }
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
    private readonly List<GameData> _gameData;
    private readonly Dictionary<string, Dictionary<int[], double>> _poissonProbabilityDictionary = new ();
    
    public PoissonService(List<GameData> gameData)
    {
        _gameData = gameData;
    }

    public List<PoissonProbability> Execute(string homeTeam, string awayTeam, string league)
    {
        var result = new List<PoissonProbability>();
        var currentSeason = AnalysePerformance(homeTeam, awayTeam, league, 2022, 2023);
        var allSeasons = AnalysePerformance(homeTeam, awayTeam, league, 2017, 2022);
        var currentSeasonHalftime = AnalyseHalftimePerformance(homeTeam, awayTeam, league, 2017, 2022);
        var allSeasonsHalftime = AnalyseHalftimePerformance(homeTeam, awayTeam, league, 2017, 2022);

        foreach (var allSeason in allSeasons)
        {
            var currentSeasonProbability = currentSeason
                .Where(i => i.Key == allSeason.Key)
                .Select(ii => ii.Value)
                .FirstOrDefault();

             result.Add(new PoissonProbability
            {
                Key = allSeason.Key,
                Probability = allSeason.Value.CalculateWeighting(currentSeasonProbability),
            });
        }

        return result;
    }
 
    private Dictionary<string, double> AnalysePerformance(
        string homeTeam, string awayTeam, string league, int startYear, int endYear)
    {
        // Retrieving the season of the league by year.
        var leagueSeason = _gameData.GetLeagueSeasonBy(startYear, endYear, league);
        
        if (!leagueSeason.TeamsAreInLeague(homeTeam, awayTeam))
            return new Dictionary<string, double>();
        
        var homeMatches = CalculateTeamStrengthBy(leagueSeason, homeTeam, true);
        var awayMatches = CalculateTeamStrengthBy(leagueSeason, awayTeam);
            
        var expectedHomeGoal = homeMatches.Attack * awayMatches.Defense * homeMatches.LeagueScored;
        var expectedAwayGoal = awayMatches.Attack * homeMatches.Defense * homeMatches.LeagueConceded;
        var probabilities = PossibleProbabilities(expectedHomeGoal, expectedAwayGoal);
                
        _poissonProbabilityDictionary.Clear();
        return probabilities;
    }
   
    private Dictionary<string, double> AnalyseHalftimePerformance(
        string homeTeam, string awayTeam, string league, int startYear, int endYear)
    {
        // Retrieving the season of the league by year.
        var leagueSeason = _gameData.GetLeagueSeasonBy(startYear, endYear, league);
        
        if (!leagueSeason.TeamsAreInLeague(homeTeam, awayTeam))
            return new Dictionary<string, double>();
        
        var homeMatches = CalculateTeamStrengthBy(leagueSeason, homeTeam, true);
        var awayMatches = CalculateTeamStrengthBy(leagueSeason, awayTeam);
            
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
                
                AddOddProbabilities(homeScore, awayScore,homePoissonProbability * awayPoissonProbability);

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
        if (homeScore + awayScore <= 3 && homeScore + awayScore > 1)
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
        if (homeScore + awayScore == 0 || homeScore + awayScore == 1)
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
        if (double.IsNaN(lambda))
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }

    
    private static TeamStrength CalculateTeamStrengthBy(IList<GameData> gameData, string team, bool atHome = false)
    {
        var currentTeamGames = gameData.GetTeamMatchesBy(team, atHome);
        var leagueGames = CalculateGoalAverage(gameData, currentTeamGames.Count, atHome);
        var currentGames =  CalculateGoalAverage(currentTeamGames, isHome: atHome);

        var attack = currentGames.Scored.Divide(leagueGames.Scored);
        var defense = currentGames.Conceded.Divide(leagueGames.Conceded);

        return new TeamStrength(attack, defense, leagueGames.Scored, leagueGames.Conceded);
    }
    
    // Compute the average goals scored and conceded for all games in the season of the given league at home
    private static PoissonAverage CalculateGoalAverage(ICollection<GameData> gameData, int count = 0, bool isHome = false)
    {
        var scored = (double)(isHome ? gameData.Sum(i => i.FTHG ?? 0) : gameData.Sum(i => i.FTAG ?? 0));
        var concededScored = (double)(isHome ? gameData.Sum(i => i.FTAG ?? 0) : gameData.Sum(i => i.FTHG ?? 0));
        var countValue = (double)(count > 0 ? count * gameData.NumberOfTeamsLeague(): gameData.Count);

        var averageScored = scored.Divide(countValue);
        var averageConcededScored = concededScored.Divide(countValue);
        
        var result = new PoissonAverage(averageScored, averageConcededScored);
        
        return result;
    }
 }
using AnalyseApp.Extensions;
using AnalyseApp.models;
using MathNet.Numerics.Distributions;

namespace AnalyseApp.Services;

public interface IPoissonService
{
    public List<MatchProbability> Execute(
        string homeTeam, string awayTeam, string league);
}
public class PoissonService : IPoissonService
{
    private readonly List<GameData> _gameData;
    private readonly List<GameData> _upComingMatch;
    private readonly Dictionary<string, Dictionary<int[], double>> _poissonProbabilityDictionary = new ();
    
    public PoissonService(List<GameData> gameData, List<GameData> upComingMatch)
    {
        _gameData = gameData;
        _upComingMatch = upComingMatch;
    }

    public List<MatchProbability> Execute(string homeTeam, string awayTeam, string league)
    {
        var result = new List<MatchProbability>();
        var currentSeason = AnalysePerformance(homeTeam, awayTeam, league, 2022, 2023);
        var allSeasons = AnalysePerformance(homeTeam, awayTeam, league, 2018, 2022);
        //var currentSeasonHalftime = AnalyseHalftimePerformance(homeTeam, awayTeam, league, 2022, 2023);
        //var currentGameBookmakers = GetBet365BookmakersValuesBy(homeTeam, awayTeam);

        foreach (var allSeason in allSeasons)
        {
            var currentSeasonProbability = currentSeason
                .Where(i => i.Key == allSeason.Key)
                .Select(ii => ii.Value)
                .FirstOrDefault();

             result.Add(new MatchProbability
            {
                Key = allSeason.Key,
                Probability = allSeason.Value.CalculateWeighting(currentSeasonProbability),
                //Bet365BookMaker = GetBet365BookMakersProbabilityBy(currentGameBookmakers, allSeason.Key)
            });
        }

       // result = result.Where(i => i.Probability >= i.Bet365BookMaker).ToList();
        return result;
    }

    private static double GetBet365BookMakersProbabilityBy(GameData gameData, string key)
    {
        return key switch
        {
            nameof(gameData.BothTeamScore) => gameData.BothTeamScore == "" ? 0.0 : Convert.ToDouble(gameData.BothTeamScore),
            nameof(gameData.MoreThanTwoGoals) => gameData.MoreThanTwoGoals ==  "" ? 0.0 : Convert.ToDouble(gameData.MoreThanTwoGoals),
            nameof(gameData.LessThanTwoGoals) => gameData.LessThanTwoGoals ==  "" ? 0.0 : Convert.ToDouble(gameData.LessThanTwoGoals),
            nameof(gameData.TwoToThree) => gameData.TwoToThree ==  "" ? 0.0 : Convert.ToDouble(gameData.TwoToThree),
            nameof(gameData.Draw) => gameData.Draw ==  "" ? 0.0 : Convert.ToDouble(gameData.Draw),
            nameof(gameData.AwayWin) => gameData.AwayWin == "" ? 0.0 : Convert.ToDouble(gameData.AwayWin),
            nameof(gameData.HomeWin) => gameData.HomeWin == "" ? 0.0 : Convert.ToDouble(gameData.HomeWin),
            _ => 0
        };
    }
    
    private GameData GetBet365BookmakersValuesBy(string homeTeam, string awayTeam)
    {
        var match = _upComingMatch
            .First(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam);
        
        return match;
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
        
        var homeMatches = CalculateTeamStrengthBy(leagueSeason, homeTeam, true, true);
        var awayMatches = CalculateTeamStrengthBy(leagueSeason, awayTeam, halftime: true);
            
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
        if (double.IsNaN(lambda))
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }

    
    private static TeamStrength CalculateTeamStrengthBy(IList<GameData> gameData, string team, bool atHome = false, bool halftime = false)
    {
        var currentTeamGames = gameData.GetTeamMatchesBy(team, atHome);
        var leagueGames = CalculateGoalAverage(gameData, currentTeamGames.Count, atHome, halftime);
        var currentGames =  CalculateGoalAverage(currentTeamGames, isHome: atHome, halftime: halftime);

        var attack = currentGames.Scored.Divide(leagueGames.Scored);
        var defense = currentGames.Conceded.Divide(leagueGames.Conceded);

        return new TeamStrength(attack, defense, leagueGames.Scored, leagueGames.Conceded);
    }
    
    // Compute the average goals scored and conceded for all games in the season of the given league at home
    private static Average CalculateGoalAverage(ICollection<GameData> gameData, int count = 0, bool isHome = false, bool halftime = false)
    {
        var scored = (double)(isHome ? gameData.Sum(i => halftime ? i.HTHG : i.FTHG ?? 0) : gameData.Sum(i => halftime ? i.HTAG : i.FTAG ?? 0));
        var concededScored = (double)(isHome ? gameData.Sum(i => halftime ? i.HTAG : i.FTAG ?? 0) : gameData.Sum(i => halftime ? i.HTHG : i.FTHG ?? 0));
        var countValue = (double)(count > 0 ? count * gameData.NumberOfTeamsLeague(): gameData.Count);

        var averageScored = scored.Divide(countValue);
        var averageConcededScored = concededScored.Divide(countValue);
        
        var result = new Average(averageScored, averageConcededScored);
        
        return result;
    }
 }
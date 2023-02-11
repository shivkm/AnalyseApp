using AnalyseApp.Extensions;
using AnalyseApp.Generics;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class MarkovChain
{
    private Dictionary<string, int> homeGoals;
    private Dictionary<string, int> awayGoals;
    private int totalGames;

    public MarkovChain()
    {
        homeGoals = new Dictionary<string, int>();
        awayGoals = new Dictionary<string, int>();
        totalGames = 0;
    }

    public void AddGame(List<HistoricalGame> pastGames)
    {
        foreach (var match in pastGames)
        {
            // Count the number of goals scored by the home team
            var key = match.HomeTeam + "-" + match.FTHG;
            if (homeGoals.ContainsKey(key))
            {
                homeGoals[key]++;
            }
            else
            {
                homeGoals[key] = 1;
            }

            // Count the number of goals scored by the away team
            key = match.AwayTeam + "-" + match.FTAG;
            if (awayGoals.ContainsKey(key))
            {
                awayGoals[key]++;
            }
            else
            {
                awayGoals[key] = 1;
            }

            totalGames++;
        }
    }

    public Tuple<double, double> PredictScore(string homeTeam, string awayTeam)
    {
        var homeGoalsScored = 0.0;
        var awayGoalsScored = 0.0;

        foreach (var homeGoal in homeGoals)
        {
            var parts = homeGoal.Key.Split('-');
            if (parts[0] == homeTeam)
            {
                homeGoalsScored += (double)(homeGoal.Value * Convert.ToInt32(parts[1])) / totalGames;
            }
        }
        
        foreach (var awayGoal in awayGoals)
        {
            var parts = awayGoal.Key.Split('-');
            if (parts[0] == awayTeam)
            {
                awayGoalsScored += (double)(awayGoal.Value * Convert.ToInt32(parts[1])) / totalGames;
            }
        }

        return Tuple.Create(homeGoalsScored, awayGoalsScored);
    }
    
}



public record GameData
{
    public string Team { get; set; }
    public string AwayTeam { get; set; }
    public int FullTimeScore { get; set; }
    public int FullTimeScoreConceded { get; set; }
    public int HalftimeScore { get; set; }
    public int HalftimeScoreConceded { get; set; }
    public int Shots { get; set; }
    public int ShotsOnGoal { get; set; }
    public int AwayShots { get; set; }
    public int AwayShotsOnGoal { get; set; }
    public int Offsides { get; set; }
    public int AwayOffsides { get; set; }
}

public class MarkovChainService
{
    private readonly IList<HistoricalGame> _currentSeason;
    private readonly IList<HistoricalGame> _lastSixSeason;
    private List<GameData> _HeadToHeadData = new ();

    public MarkovChainService(IReadOnlyCollection<HistoricalGame> gameData)
    {
        _currentSeason = gameData.GetGameDataBy(2022, 2023);
        _lastSixSeason = gameData.GetGameDataBy(2016, 2022);
    }

    public void Execute(string homeTeam, string awayTeam)
    {
        var homeTeamData = GetListOfGameData(
            _currentSeason.Where(item => item.HomeTeam == homeTeam), true);
        
        var awayTeamData = GetListOfGameData(
            _currentSeason.Where(item => item.AwayTeam == awayTeam));
        
        // Create a Markov chain for each team
        var homeTeamMarkovChain = new MarkovChain<int>();
        var awayTeamMarkovChain = new MarkovChain<int>();
        
        // Train the Markov chain for each team using historical game data
        foreach (var data in homeTeamData)
        {
            homeTeamMarkovChain.Train(data.Shots, data.FullTimeScore);
            homeTeamMarkovChain.Train(data.Offsides, data.HalftimeScore);
            homeTeamMarkovChain.Train(data.FullTimeScore, data.FullTimeScoreConceded);
            homeTeamMarkovChain.Train(data.HalftimeScore, data.FullTimeScoreConceded);
            homeTeamMarkovChain.Train(data.FullTimeScoreConceded, data.Shots);
        }

        foreach (var data in awayTeamData)
        {
            awayTeamMarkovChain.Train(data.Shots, data.FullTimeScore);
            awayTeamMarkovChain.Train(data.Offsides, data.HalftimeScore);
            awayTeamMarkovChain.Train(data.FullTimeScore, data.FullTimeScoreConceded);
            awayTeamMarkovChain.Train(data.HalftimeScore, data.FullTimeScoreConceded);
            awayTeamMarkovChain.Train(data.FullTimeScoreConceded, data.Shots);
        }
        

        // Use the trained Markov chain to make a prediction for each team
        Console.WriteLine("Next state for team A: " + homeTeamMarkovChain.Predict(1));
        Console.WriteLine("Next state for team B: " + awayTeamMarkovChain.Predict(1));

    }


    private void TZest(string homeTeam, string awayTeam)
    {
        
    }
    
    private static List<GameData> GetListOfGameData(IEnumerable<HistoricalGame> historicalGames, bool isHome = false)
    {
        var gameData = historicalGames.Select(s => new GameData
        {
            Team = isHome ? s.HomeTeam : s.AwayTeam,
            FullTimeScore = isHome ? s.FTHG ?? 0: s.FTAG ?? 0,
            FullTimeScoreConceded = isHome ? s.FTAG ?? 0: s.FTHG ?? 0,
            HalftimeScore = isHome ? s.HTHG ?? 0: s.HTAG ?? 0,
            HalftimeScoreConceded = isHome ? s.HTAG ?? 0: s.HTHG ?? 0,
            Shots = isHome ? s.HS ?? 0: s.AS ?? 0,
            ShotsOnGoal = isHome ? s.HST ?? 0: s.AST ?? 0,
            Offsides = isHome ? s.HO ?? 0: s.AO ?? 0,

        }).ToList();

        return gameData;
    }
}
using AnalyseApp.Extensions;
using AnalyseApp.Generics;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

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
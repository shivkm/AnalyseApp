using AnalyseApp.Models;

namespace AnalyseApp.Algorithm;

public class MarkovChain
{
    private readonly Dictionary<string, int> _homeGoals;
    private readonly Dictionary<string, int> _awayGoals;
    private int _totalGames;

    public MarkovChain()
    {
        _homeGoals = new Dictionary<string, int>();
        _awayGoals = new Dictionary<string, int>();
        _totalGames = 0;
    }

    public void AddHomeGamesBy(List<HistoricalGame> pastGames)
    {
        foreach (var match in pastGames)
        {
            // Count the number of goals scored by the home team
            var key = match.HomeTeam + "-" + match.FTHG;
            if (_homeGoals.ContainsKey(key))
            {
                _homeGoals[key]++;
            }
            else
            {
                _homeGoals[key] = 1;
            }
            
            _totalGames++;
        }
    }
    
    public void AddAwayGamesBy(List<HistoricalGame> pastGames)
    {
        foreach (var match in pastGames)
        {
            // Count the number of goals scored by the away team
            var key = match.AwayTeam + "-" + match.FTAG;
            if (_awayGoals.ContainsKey(key))
            {
                _awayGoals[key]++;
            }
            else
            {
                _awayGoals[key] = 1;
            }
            _totalGames++;
        }
    }
    
    public void AddGame(List<HistoricalGame> pastGames)
    {
        foreach (var match in pastGames)
        {
            // Count the number of goals scored by the home team
            var key = match.HomeTeam + "-" + match.FTHG;
            if (_homeGoals.ContainsKey(key))
            {
                _homeGoals[key]++;
            }
            else
            {
                _homeGoals[key] = 1;
            }

            // Count the number of goals scored by the away team
            key = match.AwayTeam + "-" + match.FTAG;
            if (_awayGoals.ContainsKey(key))
            {
                _awayGoals[key]++;
            }
            else
            {
                _awayGoals[key] = 1;
            }

            _totalGames++;
        }
    }

    public double PredictScore(string team)
    {
        var homeGoalsScored = 0.0;
        var awayGoalsScored = 0.0;

        foreach (var homeGoal in _homeGoals)
        {
            var parts = homeGoal.Key.Split('-');
            if (parts[0] == team)
            {
                homeGoalsScored += (double)(homeGoal.Value * Convert.ToInt32(parts[1])) / _totalGames;
            }
        }
        
        foreach (var awayGoal in _awayGoals)
        {
            var parts = awayGoal.Key.Split('-');
            if (parts[0] == team)
            {
                awayGoalsScored += (double)(awayGoal.Value * Convert.ToInt32(parts[1])) / _totalGames;
            }
        }

        return homeGoalsScored + awayGoalsScored;
    }
    
    public Tuple<double, double> PredictScore(string homeTeam, string awayTeam)
    {
        var homeGoalsScored = 0.0;
        var awayGoalsScored = 0.0;

        foreach (var homeGoal in _homeGoals)
        {
            var parts = homeGoal.Key.Split('-');
            if (parts[0] == homeTeam)
            {
                homeGoalsScored += (double)(homeGoal.Value * Convert.ToInt32(parts[1])) / _totalGames;
            }
        }
        
        foreach (var awayGoal in _awayGoals)
        {
            var parts = awayGoal.Key.Split('-');
            if (parts[0] == awayTeam)
            {
                awayGoalsScored += (double)(awayGoal.Value * Convert.ToInt32(parts[1])) / _totalGames;
            }
        }

        return Tuple.Create(homeGoalsScored, awayGoalsScored);
    }
    
}
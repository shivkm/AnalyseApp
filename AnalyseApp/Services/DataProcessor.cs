using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class DataProcessor: IDataProcessor
{
    private readonly Dictionary<string, List<float>> homeGoalsHistory = new();
    private readonly Dictionary<string, List<float>> awayGoalsHistory = new();
    
    public List<Match> CalculateMatchAveragesDataBy(List<Match> historicalMatches, Match upcomingMatch)
    {
        var teamsHistoricalMatches = historicalMatches
            .OrderMatchesBy(upcomingMatch.Date.Parse())
            .ToList();
        
        CalculateAverages(teamsHistoricalMatches);

        return teamsHistoricalMatches;
    }

    private void CalculateAverages(IEnumerable<Match> games)
    {
        foreach (var game in games)
        {
            // Update history
            UpdateHistory(game.HomeTeam, game.FullTimeHomeGoals, homeGoalsHistory);
            UpdateHistory(game.AwayTeam, game.FullTimeAwayGoals, awayGoalsHistory);

            // Calculate averages
            game.AverageHomeGoals = homeGoalsHistory[game.HomeTeam].Average();
            game.AverageAwayGoals = awayGoalsHistory[game.AwayTeam].Average();
        }
    }

    private static void UpdateHistory(string team, float goals, IDictionary<string, List<float>> history)
    {
        if (!history.TryGetValue(team, out  var value))
        {
            value = new List<float>();
            history[team] = value;
        }

        value.Add(goals);
    }
}
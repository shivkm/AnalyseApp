using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IDataProcessor
{
   // Task SaveLeague(List<ApiResponse> apiResponses);
    TeamData CalculateTeamData(IEnumerable<Match> matches, string teamName);
    List<MatchAverage> CalculateMatchAveragesDataBy(IEnumerable<Match> historicalData, DateTime upcomingMatchDate);
    MatchData GetLastSixMatchDataBy(List<Match> historicalMatches, string home, string away, DateTime playedOn);
    
    MatchAverage CalculateGoalMatchAverageBy(
        IEnumerable<Match> historicalMatches, 
        string homeTeam, 
        string awayTeam,
        DateTime playedOn,
        float homeScore,
        float awayScore,
        bool currentLeague = false
    );
    
    
}
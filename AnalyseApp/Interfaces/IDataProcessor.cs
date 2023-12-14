using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IDataProcessor
{
    TeamData CalculateTeamData(IEnumerable<Match> matches, string teamName);
    List<MatchData> CalculateMatchAveragesDataBy(IEnumerable<Match> historicalData, DateTime upcomingMatchDate);
    MatchAverage CalculateGoalMatchAverageBy(IEnumerable<Match> historicalMatches, string homeTeam, string awayTeam);
}
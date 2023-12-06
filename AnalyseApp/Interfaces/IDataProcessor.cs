using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IDataProcessor
{
    List<Match> CalculateMatchAveragesDataBy(List<Match> historicalMatches, Match upcomingMatch);
}
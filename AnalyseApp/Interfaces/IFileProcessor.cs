using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IFileProcessor
{
    List<Match> GetHistoricalMatchesBy();
    List<Match> GetUpcomingGamesBy(string fixtureFileName);
    void CreateFixtureBy(string startDate, string endDate);
}
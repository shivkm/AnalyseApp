using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IFileProcessor
{
    List<Matches> GetHistoricalMatchesBy();
    List<Matches> GetUpcomingGamesBy(string fixtureFileName);
    void CreateFixtureBy(string startDate, string endDate);
}
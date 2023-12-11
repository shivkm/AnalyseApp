using System.Text.RegularExpressions;

namespace ScoreMind.Interfaces;

public interface IFileProcessor
{
    List<Match> GetHistoricalGamesBy();
    List<Match> GetUpcomingGamesBy(string fixtureFileName);
    void CreateFixtureBy(string startDate, string endDate);
}
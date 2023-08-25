using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IFileProcessor
{
    List<Matches> GetHistoricalMatchesBy();
    List<Game> GetHistoricalGames();
    List<Game> MapMatchesToGames(IEnumerable<Matches> matches);
    void CreateCsvFile(IEnumerable<Game> games);
}
using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IFileProcessor
{
    List<Matches> GetHistoricalMatchesBy();
    List<GameAverage> GetMatchFactors();
    List<Game> GetHistoricalGames();
    List<Game> MapMatchesToGames(List<Matches> matches);
    void CreateCsvFile(List<Game> games);
}
using PredictionTool.Models;

namespace PredictionTool.Interfaces;

public interface IFileProcessor
{
    Task CreateHistoricalGamesFile(CancellationToken token);
    Task CreateUpcomingFixtureBy(CancellationToken token);
    List<Game> GetHistoricalGamesBy(DateTime endDate);
    List<Game> GetUpcomingGamesBy(DateTime endDate);
}
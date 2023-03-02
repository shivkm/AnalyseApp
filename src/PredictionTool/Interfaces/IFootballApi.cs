using PredictionTool.Enums;
using PredictionTool.Models;

namespace PredictionTool.Interfaces;

public interface IFootballApi
{
    Task<List<Game>?> GetUpcomingMatchesBy(League league, int matchDay, CancellationToken token);
}
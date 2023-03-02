using PredictionTool.Models;

namespace PredictionTool.Interfaces;

public interface ITeamStrengthCalculator
{
    List<GameProbability> Calculate(List<Game> historicalGames, string homeTeam, string awayTeam, string league);
}
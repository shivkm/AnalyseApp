using PredictionTool.Models;

namespace PredictionTool.Interfaces;

public interface ICalculatorService
{
    List<GameProbability> Calculate(List<Game> historicalGames, string homeTeam, string awayTeam, string league);
}
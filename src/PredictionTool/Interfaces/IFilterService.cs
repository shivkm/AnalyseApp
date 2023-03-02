using PredictionTool.Models;

namespace PredictionTool.Interfaces;

public interface IFilterService
{
    (string Key, double Probability) FilterGames(
        QualifiedGames qualifiedGames,
        List<GameProbability> gameProbabilities, 
        List<Game> historicalGames
    );
}
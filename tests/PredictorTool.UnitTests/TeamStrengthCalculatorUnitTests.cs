using FluentAssertions;
using NSubstitute;
using PredictionTool.Interfaces;
using PredictionTool.Models;
using PredictionTool.Services;

namespace PredictorTool.UnitTests;

public class TeamStrengthCalculatorUnitTests
{
    [Fact]
    public void GivenListOfGames_WhenGoalAverageCalculated_ThenTheCalculatedAverageIsAsExpected()
    {
        // ARRANGE
        var mockGames = GetMockedGamesBy("Man United");
        var fileProcessor = Substitute.For<IFileProcessor>();
        var calcService = new CalculatorService(fileProcessor);
        
        // ACT
        var act = CalculatorService.PossibleProbabilities(0.64, 2.10);
        
        // ASSERT
    }

    private List<Game> GetMockedGamesBy(string team)
    {
        var result = new List<Game>();
        var random = new Random();
        
        for (var i = 0; i < 15; i++)
        {
            result.Add(new Game
            {
                Home = team,
                Away = "",
                FullTimeHomeScore = random.Next(0, 4),
                FullTimeAwayScore = random.Next(0, 4) 
            });
        }

        return result;
    }
}
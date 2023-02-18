using AnalyseApp;
using AnalyseApp.Models;
using AnalyseApp.Services;
using FluentAssertions;

namespace AnalyseAppUnitTests;

public class FilterUnitTests: TestSetup
{
    [Fact]
    public void GivenTestGameData_WhenNoGameScoreFilterCalled_ThenTheAverageShouldLessThan15Percent()
    {
        // ARRANGE
        const string homeTeam = "FC Koln";
        const string awayTeam = "RB Leipzig";
        var mockData = GetMockDataBy(homeTeam, awayTeam).ToList();
        var halftimeGoalService = new NoGoalGameHandler();
        var gameQualification = new GameQualification
        {
            Home = new TeamQualification(),
            Away = new TeamQualification()
        };
        
        // ACT
        halftimeGoalService.HandleRequest(mockData, gameQualification, homeTeam, awayTeam);
        
        // ASSERT
        gameQualification.Home.NoGoalScoredByTeamAverage.Should().BeLessThan(0.15);
        gameQualification.Away.NoGoalScoredByTeamAverage.Should().BeLessThan(0.15);
    }
    
    [Fact]
    public void GivenTestGameData_WhenHalfTimeGoalFilterCalled_ThenTheGameAverageShouldGreaterThan50Percent()
    {
        // ARRANGE
        const string homeTeam = "FC Koln";
        const string awayTeam = "RB Leipzig";
        var mockData = GetMockDataBy(homeTeam, awayTeam).ToList();
        var halftimeGoalService = new HalftimeGoalHandler();
        var gameQualification = new GameQualification
        {
            Home = new TeamQualification(),
            Away = new TeamQualification()
        };
        
        // ACT
        halftimeGoalService.HandleRequest(mockData, gameQualification, homeTeam, awayTeam);
        
        // ASSERT
        gameQualification.Home.HalftimeScoreAverage.Should().BeGreaterThan(0.50);
        gameQualification.Away.HalftimeScoreAverage.Should().BeGreaterThan(0.50);
        gameQualification.Home.HalfTimeProbability.Should().BeGreaterThan(0.32);
        gameQualification.Away.HalfTimeProbability.Should().BeGreaterThan(0.32);
    }
    
    
    [Fact]
    public void GivenTestGameData_WhenMarkovChainProbabilityCalled_ThenTheProbabilityShouldBeGreaterThan50Percent()
    {
        // ARRANGE
        const string homeTeam = "FC Koln";
        const string awayTeam = "RB Leipzig";
        var mockData = GetMockDataBy(homeTeam, awayTeam).ToList();
        var halftimeGoalService = new MarkovChainHandler();
        var gameQualification = new GameQualification
        {
            Home = new TeamQualification(),
            Away = new TeamQualification()
        };
        
        // ACT
        halftimeGoalService.HandleRequest(mockData, gameQualification, homeTeam, awayTeam);
        
        // ASSERT
        gameQualification.Home.MarkovChainAtLeastOneGoalProbability.Should().BeGreaterThan(0.60);
        gameQualification.Away.MarkovChainAtLeastOneGoalProbability.Should().BeGreaterThan(0.60);
    }
}
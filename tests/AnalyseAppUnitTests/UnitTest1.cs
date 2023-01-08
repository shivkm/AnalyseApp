using AnalyseApp;
using FluentAssertions;

namespace AnalyseAppUnitTests;

public class UnitTest1
{
    [Fact]
    public void GivenEnoughData_WhenAnalysisExecute_ThenTheResultShouldBeExpected()
    {
        // ARRANGE
        var analysis = new Analyse();
        analysis.ReadFilesHistoricalGames();
        
        // ACT
        var result = analysis.AnalyseBy("Juventus", "Udinese");
        
        // ASSERT
        result.BothTeamScore?.Qualified.Should().Be(false);

    }
}
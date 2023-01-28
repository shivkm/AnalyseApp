using AnalyseApp;
using AnalyseApp.Services;

namespace AnalyseAppUnitTests;

public class AnalyseServiceUnitTest
{
    [Fact]
    public void AnalyseLastSixSeasonGames_WithGivenParameters_ResultShouldBeExpected()
    {
        // ARRANGE
        var analysis = new Analyse();
        var games = analysis.ReadFilesHistoricalGames().GetList();
        //var analyseService = new AnalyseService(games);

        // ACT
      //  var act = analyseService.AnalyseGameBy("Juventus", "Udinese");
        
        // ASSERT
    }
}
using TechTalk.SpecFlow;

namespace PredictionTool.Tests.Steps;

[Binding]
public class TeamStrengthCalculator
{
    
    [Given(@"(.*) played matched in league")]
    public void GivenPlayedMatchedInLeague(int p0)
    {
        var program = new Program();
    }
    
}
using AnalyseApp.Constants;
using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Options;
using AnalyseApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace AnalyseApp.Tests;

public class LeagueGoalsUnitTests
{
    private readonly IFileProcessor _fileProcessor;
    private readonly IMatchPredictor _matchPredictor;
    private readonly ITestOutputHelper _testOutputHelper;
    
    public LeagueGoalsUnitTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions
        {
            RawCsvDir = "/Users/shivm/Documents/projects/AnalyseApp/data/raw_csv",
            Upcoming = "/Users/shivm/Documents/projects/AnalyseApp/data/upcoming"
        };

        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        _fileProcessor = new FileProcessor(optionsWrapper);
        
        _matchPredictor = new MatchPredictor(_fileProcessor, new PoissonService(), new DataService(_fileProcessor));
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void GivenRawData_WhenTeamsGoalsQueried_ThenTheResponseIsAsExpected()
    {
        var expectedTeamSeasonGoals = 30;
        var upcomingMatch = new Matches
        {
            HomeTeam = PremierLeague.SheffieldUnited,
            AwayTeam = PremierLeague.ManUnited,
            Date = "21/10/2023",
            FTHG = 1,
            FTAG = 2
        };

        // ACTUAL ASSERT
        var goalsData = _matchPredictor.Execute(
            upcomingMatch, BetType.Unknown
        );
        

    }
}
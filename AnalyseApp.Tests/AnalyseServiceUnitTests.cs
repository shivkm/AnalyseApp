using System.ComponentModel;
using Accord;
using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Options;
using AnalyseApp.Services;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace AnalyseApp.Tests;

public class AnalyseServiceUnitTests
{
    private readonly IMatchPredictor _matchPredictor;
    private readonly ITestOutputHelper _testOutputHelper;

    public AnalyseServiceUnitTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions 
        {
            RawCsvDir = "C:\\shivm\\AnalyseApp\\data\\raw_csv",
            AnalyseResult = "C:\\shivm\\AnalyseApp\\data\\analysed_result"
        };

        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        var fileProcessor = new FileProcessor(optionsWrapper);
        var historicalData = fileProcessor.GetHistoricalMatchesBy();
        
        _matchPredictor = new MatchPredictor(historicalData, new PoissonService(), new DataService(historicalData));
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void Premier_League_Game_State_Preparation()
    {
        // ARRANGE
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = "Werder Bremen", AwayTeam = "Bayern Munich", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Kaiserlautern", AwayTeam = "Elversberg", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Wehen", AwayTeam = "Karlsruhe", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = PremierLeague.NottmForest.GetDescription(), AwayTeam = "Sheffield United", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Leeds", AwayTeam = "West Brom", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Mallorca", AwayTeam = "Villarreal", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Valencia", AwayTeam = "Las Palmas", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Andorra", AwayTeam = "Cartagena", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Zaragoza", AwayTeam = "Valladolid", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Metz", AwayTeam = "Marseille", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Bari", AwayTeam = "Palermo", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
        };
       
        // ACTUAL ASSERT
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam}{actual.Msg}");
        }
    }
    
    [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void Premier_League_Game_State_Preparation2()
    {
        // ARRANGE
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = "Werder Bremen", AwayTeam = "Bayern Munich", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Kaiserlautern", AwayTeam = "Elversberg", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Wehen", AwayTeam = "Karlsruhe", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = PremierLeague.NottmForest.GetDescription(), AwayTeam = "Sheffield United", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Leeds", AwayTeam = "West Brom", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Mallorca", AwayTeam = "Villarreal", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Valencia", AwayTeam = "Las Palmas", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Andorra", AwayTeam = "Cartagena", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Zaragoza", AwayTeam = "Valladolid", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Metz", AwayTeam = "Marseille", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Bari", AwayTeam = "Palermo", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Reims", AwayTeam = "Clermont", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Fulham", AwayTeam = "Brentford", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Liverpool", AwayTeam = "Bournemouth", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Luton", AwayTeam = "Burnley", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Wolves", AwayTeam = "Brighton", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Tottenham", AwayTeam = "Man United", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Man City", AwayTeam = "Newcastle", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Empoli", AwayTeam = "Verona", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Frosinone", AwayTeam = "Napoli", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
        };
       
        // ACTUAL ASSERT
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam}{actual.Msg}");
        }
    }
}
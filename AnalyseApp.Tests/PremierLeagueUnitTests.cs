using System.ComponentModel;
using AnalyseApp.Constants;
using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Options;
using AnalyseApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace AnalyseApp.Tests;

public class PremierLeagueUnitTests
{
    private readonly IFileProcessor _fileProcessor;
    private readonly IMatchPredictor _matchPredictor;
    private readonly ITestOutputHelper _testOutputHelper;

    private int totalCount = 0;
    private int correctCount = 0;
    private int wrongCount = 0;
    
    private const string overTwoGoals = "Over Tow Goals";
    private const string underThreeGoals = "Under Three Goals";
    private const string bothTeamScore = "Both Team Score Goals";
    private const string twoToThreeGoals = "Two to three Goals";
    private const string HomeWin = "Home will win";
    private const string AwayWin = "Away will win";
    private const string BothTeamScore = "Both Team Score Goals";

    public PremierLeagueUnitTests(ITestOutputHelper testOutputHelper)
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

    [
        Theory(DisplayName = "Premier league predictions"), 
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-9-9"),
        InlineData("fixture-16-9"),
        InlineData("fixture-23-9"),
        InlineData("fixture-30-9"),
        InlineData("fixture-7-10"),
        InlineData("fixture-14-10"),

    ]
    public void PremierLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.Div == "E0")
            .ToList();

        if (premierLeagueMatches.Count() is 0) return;
        
        // ACTUAL 
        foreach (var matches in premierLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} scoring power: {actual.Percentage:F}";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            totalCount++;
        }

        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount}  accuracy rate: {accuracyRate:F}");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(75);
    }
    
    [
        Theory(DisplayName = "Spanish league predictions"), 
       // InlineData("fixture-11-8"),
       // InlineData("fixture-18-8"),
     //   InlineData("fixture-25-8"),
     //   InlineData("fixture-01-9"),
     //   InlineData("fixture-15-9"),
    //    InlineData("fixture-22-9"),
        InlineData("fixtures"),
    ]
    public void SpanishLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var spanishLeagueMatches = fixture.Where(i => i.Div == "I2");

        // ACTUAL 
        foreach (var matches in spanishLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} scoring power: {actual.Percentage:F}";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            totalCount++;
        }

        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount}  accuracy rate: {accuracyRate:F}");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(70);
    }

        
    [Theory(DisplayName = "Both team score goal predictions"), 
     InlineData("fixture-11-8"),
     InlineData("fixture-18-8"),
     InlineData("fixture-25-8")
    ]
    public void Both_Team_Score_Goal_Logic_should_Have_Accuracy_Rate_More_Then_Eighty_Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);

        // ACTUAL ASSERT
        foreach (var lastSixGame in fixture)
        {
            totalCount++;
            var actual = _matchPredictor.Execute(
                lastSixGame, BetType.BothTeamScoreGoals
            );
            var isCorrect = lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals;

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");

        accuracyRate.Should().BeGreaterThan(80);
    }
            
    [Fact]
    public void Over_Two_Goal_Logic_should_Have_Accuracy_Rate_More_Then_Eighty_Percent()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = FranceLeague.Rennes, AwayTeam = FranceLeague.Lille, Date = "16/09/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = PremierLeague.AstonVilla, AwayTeam = PremierLeague.CrystalPalace, Date = "16/09/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = PremierLeague.Wolves, AwayTeam = PremierLeague.Liverpool, Date = "16/09/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Fulham, AwayTeam = PremierLeague.Luton, Date = "16/09/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = PremierLeague.ManUnited, AwayTeam = PremierLeague.Brighton, Date = "16/09/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Liverpool, AwayTeam = PremierLeague.Bournemouth, Date = "19/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Wolves, AwayTeam = PremierLeague.Brighton, Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = PremierLeague.Forest, AwayTeam = PremierLeague.SheffieldUnited, Date = "18/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.ManUnited, AwayTeam = PremierLeague.Forest, Date = "26/08/2023", FTHG = 3, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Brighton, AwayTeam = PremierLeague.WestHam, Date = "26/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = PremierLeague.Burnley, AwayTeam = PremierLeague.AstonVilla, Date = "27/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = PremierLeague.SheffieldUnited, AwayTeam = PremierLeague.ManCity, Date = "27/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Newcastle, AwayTeam = PremierLeague.Liverpool, Date = "27/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Arsenal, AwayTeam = PremierLeague.Fulham, Date = "26/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Nantes, AwayTeam = FranceLeague.Monaco, Date = "25/08/2023", FTHG = 3, FTAG = 3 },
            new() { HomeTeam = FranceLeague.ParisSg, AwayTeam = FranceLeague.Lens, Date = "26/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Rennes, AwayTeam = FranceLeague.LeHavre, Date = "27/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Montpellier, AwayTeam = FranceLeague.Reims, Date = "27/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = FranceLeague.Lorient, AwayTeam = FranceLeague.Lille, Date = "27/08/2023", FTHG = 4, FTAG = 1 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ over two goals logic ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            totalCount++;
            var actual = _matchPredictor.Execute(
                lastSixGame, BetType.OverTwoGoals
            );
            var isCorrect = lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals;

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Type} scoring power: {actual.Percentage:F} - ✅ - {actual.Msg}");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");

        accuracyRate.Should().BeGreaterThan(80);
    }
                
    [Fact]
    public void Two_To_Three_Logic_should_Have_Accuracy_Rate_More_Then_Eighty_Percent()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = Championship.Millwall, AwayTeam = Championship.Leeds, Date = "17/09/2023", FTHG = 2, FTAG = 0 }, 
            new() { HomeTeam = FranceLeague.Toulouse, AwayTeam = FranceLeague.ParisSg, Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Lille, AwayTeam = FranceLeague.Nantes, Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.LeHavre, AwayTeam = FranceLeague.Brest, Date = "20/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Reims, AwayTeam = FranceLeague.Clermont, Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Lorient, AwayTeam = FranceLeague.Nice, Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Monaco, AwayTeam = FranceLeague.Strasbourg, Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Lens, AwayTeam = FranceLeague.Rennes, Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Strasbourg, AwayTeam = FranceLeague.Toulouse, Date = "27/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Montpellier, AwayTeam = FranceLeague.Reims, Date = "27/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = FranceLeague.Marseille, AwayTeam = FranceLeague.Brest, Date = "26/08/2023", FTHG = 2, FTAG = 0 },

        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ two to three goals logic ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            totalCount++;
            var actual = _matchPredictor.Execute(
                lastSixGame, BetType.TwoToThreeGoals
            );
            var isCorrect = lastSixGame.FTAG + lastSixGame.FTHG is 2 or 3 && actual.Type == BetType.TwoToThreeGoals;

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");

        accuracyRate.Should().BeGreaterThan(80);
    }
    
    [Fact]
    public void Home_Win_Logic_should_Have_Accuracy_Rate_More_Then_Eighty_Percent()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = PremierLeague.Arsenal, AwayTeam = PremierLeague.Forest, Date = "12/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Forest, AwayTeam = PremierLeague.SheffieldUnited, Date = "18/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Bournemouth, AwayTeam = PremierLeague.Tottenham, Date = "26/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Tottenham, AwayTeam = PremierLeague.ManUnited, Date = "19/08/2023", FTHG = 2, FTAG = 0 }, 
            new() { HomeTeam = FranceLeague.Lille, AwayTeam = FranceLeague.Nantes, Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Reims, AwayTeam = FranceLeague.Clermont, Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = PremierLeague.Newcastle, AwayTeam = PremierLeague.AstonVilla, Date = "12/08/2023", FTHG = 5, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Lorient, AwayTeam = FranceLeague.Nice, Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Strasbourg, AwayTeam = FranceLeague.Toulouse, Date = "27/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Marseille, AwayTeam = FranceLeague.Brest, Date = "26/08/2023", FTHG = 2, FTAG = 0 },

        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ home win logic ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            totalCount++;
            var actual = _matchPredictor.Execute(
                lastSixGame, BetType.HomeWin
            );
            var isCorrect = lastSixGame.FTAG < lastSixGame.FTHG && actual.Type == BetType.HomeWin;

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");

        accuracyRate.Should().BeGreaterThan(80);
    }
    
    private static bool GetTheCorrectResult(Matches match, BetType betType)
    {
        return betType switch
        {
            BetType.OverTwoGoals when match.FTAG + match.FTHG > 2 => true,
            BetType.BothTeamScoreGoals when match is { FTHG: > 0, FTAG: > 0 } => true,
            BetType.UnderThreeGoals when match.FTAG + match.FTHG < 3 => true,
            BetType.TwoToThreeGoals when match.FTAG + match.FTHG is 2 or 3 => true,
            BetType.HomeWin when match.FTHG > match.FTAG => true,
            BetType.AwayWin when match.FTHG < match.FTAG => true,
            _ => false
        };
    }
}
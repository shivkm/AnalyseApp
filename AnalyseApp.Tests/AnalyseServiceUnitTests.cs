using System.ComponentModel;
using Accord;
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

public class AnalyseServiceUnitTests
{
    private readonly IMatchPredictor _matchPredictor;
    private readonly ITestOutputHelper _testOutputHelper;
    
    private const string overTwoGoals = "Over Tow Goals";
    private const string underThreeGoals = "Under Three Goals";
    private const string bothTeamScore = "Both Team Score Goals";
    private const string twoToThreeGoals = "Two to three Goals";
    private const string HomeWin = "Home will win";
    private const string AwayWin = "Away will win";
    private const string BothTeamScore = "Both Team Score Goals";

    public AnalyseServiceUnitTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions 
        {
            RawCsvDir = "C:\\shivm\\AnalyseApp\\data\\raw_csv",
            AnalyseResult = "C:\\shivm\\AnalyseApp\\data\\analysed_result"
        };

        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        var fileProcessor = new FileProcessor(optionsWrapper);
        
        _matchPredictor = new MatchPredictor(fileProcessor, new PoissonService(), new DataService(fileProcessor));
        _testOutputHelper = testOutputHelper;
    }

    [Fact, Description("Premier league first game day")]
    public void Premier_League_First_Game_Day()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = PremierLeague.Burnley, AwayTeam = PremierLeague.ManCity, Date = "11/08/2023", FTHG = 0, FTAG = 3 },
            new() { HomeTeam = PremierLeague.Arsenal, AwayTeam = PremierLeague.Forest, Date = "12/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Bournemouth, AwayTeam = PremierLeague.WestHam, Date = "12/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Brighton, AwayTeam = PremierLeague.Luton, Date = "12/08/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Everton, AwayTeam = PremierLeague.Fulham, Date = "12/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = PremierLeague.SheffieldUnited, AwayTeam = PremierLeague.CrystalPalace, Date = "12/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Newcastle, AwayTeam = PremierLeague.AstonVilla, Date = "12/08/2023", FTHG = 5, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Brentford, AwayTeam = PremierLeague.Tottenham, Date = "13/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Chelsea, AwayTeam = PremierLeague.Liverpool, Date = "13/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = PremierLeague.ManUnited, AwayTeam = PremierLeague.Wolves, Date = "13/08/2023", FTHG = 1, FTAG = 0 },
        };

        // ACTUAL ASSERT
        
        _testOutputHelper.WriteLine(" ------------------ Premier league first day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = (lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals) ||
                            (lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals) ||
                            (lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals) ||
                            ((lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3) &&
                             actual.Type == BetType.TwoToThreeGoals) ||
                            (lastSixGame.FTAG > lastSixGame.FTHG && actual.Type == BetType.AwayWin) ||
                            (lastSixGame.FTHG > lastSixGame.FTAG && actual.Type == BetType.HomeWin);

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
            totalCount++;
        }

        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount}  accuracy rate: {accuracyRate:F}");
    }
    
    [Fact, Description("Premier league second game day")]
    public void Premier_League_Second_Game_Day()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = PremierLeague.Forest, AwayTeam = PremierLeague.SheffieldUnited, Date = "18/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Fulham, AwayTeam = PremierLeague.Brentford, Date = "19/08/2023", FTHG = 0, FTAG = 3 },
            new() { HomeTeam = PremierLeague.Liverpool, AwayTeam = PremierLeague.Bournemouth, Date = "19/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Wolves, AwayTeam = PremierLeague.Brighton, Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = PremierLeague.Tottenham, AwayTeam = PremierLeague.ManUnited, Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = PremierLeague.ManCity, AwayTeam = PremierLeague.Newcastle, Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = PremierLeague.AstonVilla, AwayTeam = PremierLeague.Everton, Date = "20/08/2023", FTHG = 4, FTAG = 0 },
            new() { HomeTeam = PremierLeague.WestHam, AwayTeam = PremierLeague.Chelsea, Date = "20/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = PremierLeague.CrystalPalace, AwayTeam = PremierLeague.Arsenal, Date = "21/08/2023", FTHG = 0, FTAG = 1 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ Premier league second day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = (lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals) ||
                            (lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals) ||
                            (lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals) ||
                            ((lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3) &&
                             actual.Type == BetType.TwoToThreeGoals) ||
                            (lastSixGame.FTAG > lastSixGame.FTHG && actual.Type == BetType.AwayWin) ||
                            (lastSixGame.FTHG > lastSixGame.FTAG && actual.Type == BetType.HomeWin);

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
            totalCount++;
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");
    }
    
    [Fact, Description("Premier league third game day")]
    public void Premier_League_Third_Game_Day()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = PremierLeague.Chelsea, AwayTeam = PremierLeague.Luton, Date = "25/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = PremierLeague.Bournemouth, AwayTeam = PremierLeague.Tottenham, Date = "26/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Arsenal, AwayTeam = PremierLeague.Fulham, Date = "26/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Brentford, AwayTeam = PremierLeague.CrystalPalace, Date = "26/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Everton, AwayTeam = PremierLeague.Wolves, Date = "26/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = PremierLeague.ManUnited, AwayTeam = PremierLeague.Forest, Date = "26/08/2023", FTHG = 3, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Brighton, AwayTeam = PremierLeague.WestHam, Date = "26/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = PremierLeague.Burnley, AwayTeam = PremierLeague.AstonVilla, Date = "27/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = PremierLeague.SheffieldUnited, AwayTeam = PremierLeague.ManCity, Date = "27/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Newcastle, AwayTeam = PremierLeague.Liverpool, Date = "27/08/2023", FTHG = 1, FTAG = 2 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ Premier league third day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = (lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals) ||
                            (lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals) ||
                            (lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals) ||
                            ((lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3) &&
                             actual.Type == BetType.TwoToThreeGoals) ||
                            (lastSixGame.FTAG > lastSixGame.FTHG && actual.Type == BetType.AwayWin) ||
                            (lastSixGame.FTHG > lastSixGame.FTAG && actual.Type == BetType.HomeWin);

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
            totalCount++;
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");
    }
    
    
    [Fact, Description("Premier league third game day")]
    public void France_league_First_Game_Day()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = FranceLeague.Nice, AwayTeam = FranceLeague.Lille, Date = "11/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Marseille, AwayTeam = FranceLeague.Reims, Date = "12/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = FranceLeague.ParisSg, AwayTeam = FranceLeague.Lorient, Date = "12/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Brest, AwayTeam = FranceLeague.Lens, Date = "13/08/2023", FTHG = 3, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Clermont, AwayTeam = FranceLeague.Monaco, Date = "13/08/2023", FTHG = 2, FTAG = 4 },
            new() { HomeTeam = FranceLeague.Nantes, AwayTeam = FranceLeague.Toulouse, Date = "13/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Montpellier, AwayTeam = FranceLeague.LeHavre, Date = "13/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Rennes, AwayTeam = FranceLeague.Metz, Date = "13/08/2023", FTHG = 5, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Strasbourg, AwayTeam = FranceLeague.Lyon, Date = "13/08/2023", FTHG = 2, FTAG = 1 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ France league first day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = (lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals) ||
                            (lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals) ||
                            (lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals) ||
                            ((lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3) &&
                             actual.Type == BetType.TwoToThreeGoals) ||
                            (lastSixGame.FTAG > lastSixGame.FTHG && actual.Type == BetType.AwayWin) ||
                            (lastSixGame.FTHG > lastSixGame.FTAG && actual.Type == BetType.HomeWin);

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
            totalCount++;
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");
    }
    
    
    
    [Fact]
    public void France_league_Second_Game_Day()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = FranceLeague.Metz, AwayTeam = FranceLeague.Marseille, Date = "18/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Lyon, AwayTeam = FranceLeague.Montpellier, Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = FranceLeague.Toulouse, AwayTeam = FranceLeague.ParisSg, Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Lille, AwayTeam = FranceLeague.Nantes, Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.LeHavre, AwayTeam = FranceLeague.Brest, Date = "20/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Reims, AwayTeam = FranceLeague.Clermont, Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Lorient, AwayTeam = FranceLeague.Nice, Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Monaco, AwayTeam = FranceLeague.Strasbourg, Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Lens, AwayTeam = FranceLeague.Rennes, Date = "20/08/2023", FTHG = 1, FTAG = 1 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ France league second day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = (lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals) ||
                            (lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals) ||
                            (lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals) ||
                            ((lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3) &&
                             actual.Type == BetType.TwoToThreeGoals) ||
                            (lastSixGame.FTAG > lastSixGame.FTHG && actual.Type == BetType.AwayWin) ||
                            (lastSixGame.FTHG > lastSixGame.FTAG && actual.Type == BetType.HomeWin);

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
            totalCount++;
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");
    }
    
        
    [Fact]
    public void France_league_Third_Game_Day()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = FranceLeague.Nantes, AwayTeam = FranceLeague.Monaco, Date = "25/08/2023", FTHG = 3, FTAG = 3 },
            new() { HomeTeam = FranceLeague.Marseille, AwayTeam = FranceLeague.Brest, Date = "26/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.ParisSg, AwayTeam = FranceLeague.Lens, Date = "26/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Rennes, AwayTeam = FranceLeague.LeHavre, Date = "27/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Montpellier, AwayTeam = FranceLeague.Reims, Date = "27/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = FranceLeague.Clermont, AwayTeam = FranceLeague.Metz, Date = "27/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Strasbourg, AwayTeam = FranceLeague.Toulouse, Date = "27/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Lorient, AwayTeam = FranceLeague.Lille, Date = "27/08/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Nice, AwayTeam = FranceLeague.Lyon, Date = "27/08/2023", FTHG = 0, FTAG = 0 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ France league third day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = (lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals) ||
                            (lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals) ||
                            (lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals) ||
                            ((lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3) &&
                             actual.Type == BetType.TwoToThreeGoals) ||
                            (lastSixGame.FTAG > lastSixGame.FTHG && actual.Type == BetType.AwayWin) ||
                            (lastSixGame.FTHG > lastSixGame.FTAG && actual.Type == BetType.HomeWin);

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
            totalCount++;
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");
    }
    
        
    [Fact]
    public void Both_Team_Score_Goal_Logic_should_Have_Accuracy_Rate_More_Then_Eighty_Percent()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = PremierLeague.Newcastle, AwayTeam = PremierLeague.AstonVilla, Date = "12/08/2023", FTHG = 5, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Brentford, AwayTeam = PremierLeague.Tottenham, Date = "13/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Chelsea, AwayTeam = PremierLeague.Liverpool, Date = "13/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Arsenal, AwayTeam = PremierLeague.Forest, Date = "12/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Bournemouth, AwayTeam = PremierLeague.WestHam, Date = "12/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Brighton, AwayTeam = PremierLeague.Luton, Date = "12/08/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = PremierLeague.WestHam, AwayTeam = PremierLeague.Chelsea, Date = "20/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Liverpool, AwayTeam = PremierLeague.Bournemouth, Date = "19/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Wolves, AwayTeam = PremierLeague.Brighton, Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = PremierLeague.Forest, AwayTeam = PremierLeague.SheffieldUnited, Date = "18/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.ManUnited, AwayTeam = PremierLeague.Forest, Date = "26/08/2023", FTHG = 3, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Brighton, AwayTeam = PremierLeague.WestHam, Date = "26/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = PremierLeague.Burnley, AwayTeam = PremierLeague.AstonVilla, Date = "27/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = PremierLeague.SheffieldUnited, AwayTeam = PremierLeague.ManCity, Date = "27/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Newcastle, AwayTeam = PremierLeague.Liverpool, Date = "27/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Arsenal, AwayTeam = PremierLeague.Fulham, Date = "26/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Brentford, AwayTeam = PremierLeague.CrystalPalace, Date = "26/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Nantes, AwayTeam = FranceLeague.Monaco, Date = "25/08/2023", FTHG = 3, FTAG = 3 },
            new() { HomeTeam = FranceLeague.ParisSg, AwayTeam = FranceLeague.Lens, Date = "26/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Rennes, AwayTeam = FranceLeague.LeHavre, Date = "27/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = FranceLeague.Montpellier, AwayTeam = FranceLeague.Reims, Date = "27/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = FranceLeague.Lorient, AwayTeam = FranceLeague.Lille, Date = "27/08/2023", FTHG = 4, FTAG = 1 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ France league third day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            totalCount++;
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date, BetType.BothTeamScoreGoals
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
            new() { HomeTeam = PremierLeague.Newcastle, AwayTeam = PremierLeague.AstonVilla, Date = "12/08/2023", FTHG = 5, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Brentford, AwayTeam = PremierLeague.Tottenham, Date = "13/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Arsenal, AwayTeam = PremierLeague.Forest, Date = "12/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Brighton, AwayTeam = PremierLeague.Luton, Date = "12/08/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = PremierLeague.WestHam, AwayTeam = PremierLeague.Chelsea, Date = "20/08/2023", FTHG = 3, FTAG = 1 },
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
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date, BetType.OverTwoGoals
            );
            var isCorrect = lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals;

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
    public void Two_To_Three_Logic_should_Have_Accuracy_Rate_More_Then_Eighty_Percent()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = PremierLeague.Arsenal, AwayTeam = PremierLeague.Forest, Date = "12/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Forest, AwayTeam = PremierLeague.SheffieldUnited, Date = "18/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Newcastle, AwayTeam = PremierLeague.Liverpool, Date = "27/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Bournemouth, AwayTeam = PremierLeague.Tottenham, Date = "26/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = PremierLeague.Tottenham, AwayTeam = PremierLeague.ManUnited, Date = "19/08/2023", FTHG = 2, FTAG = 0 }, 
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
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date, BetType.TwoToThreeGoals
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
            new() { HomeTeam = FranceLeague.Lorient, AwayTeam = FranceLeague.Nice, Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = FranceLeague.Monaco, AwayTeam = FranceLeague.Strasbourg, Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Strasbourg, AwayTeam = FranceLeague.Toulouse, Date = "27/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = FranceLeague.Marseille, AwayTeam = FranceLeague.Brest, Date = "26/08/2023", FTHG = 2, FTAG = 0 },

        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ home win logic ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            totalCount++;
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date, BetType.HomeWin
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
    
    [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void Test_18_20_August_Prediction()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = Bundesliga.Bremen, AwayTeam = Bundesliga.Bayern, Date = "18/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = Bundesliga.Hoffenheim, AwayTeam = Bundesliga.Freiburg, Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = Bundesliga.Union, AwayTeam = Bundesliga.Mainz, Date = "18/08/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = Bundesliga.Frankfurt, AwayTeam = Bundesliga.Darmstadt, Date = "18/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Hansa Rostock", AwayTeam = "Hannover", Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Osnabruck", AwayTeam = "Nurnberg", Date = "19/08/2023", FTHG = 2, FTAG = 3 },
            new() { HomeTeam = "Holstein Kiel", AwayTeam = "Magdeburg", Date = "19/08/2023", FTHG = 2, FTAG = 4 },
            new() { HomeTeam = "Liverpool", AwayTeam = "Bournemouth", Date = "18/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = "Tottenham", AwayTeam = "Man United", Date = "18/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "West Ham", AwayTeam = "Chelsea", Date = "19/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = "Crystal Palace", AwayTeam = "Arsenal", Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Blackburn", AwayTeam = "Hull", Date = "19/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Hamburg", AwayTeam = "Hertha", Date = "19/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Fulham", AwayTeam = "Brentford", Date = "19/08/2023", FTHG = 0, FTAG = 3 },
            new() { HomeTeam = "Wolves", AwayTeam = "Brighton", Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = "Man City", AwayTeam = "Newcastle", Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Aston Villa", AwayTeam = "Everton", Date = "20/08/2023", FTHG = 4, FTAG = 0 },
            new() { HomeTeam = "Plymouth", AwayTeam = "Southampton", Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Bristol City", AwayTeam = "Birmingham", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Leicester", AwayTeam = "Cardiff", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Middlesbrough", AwayTeam = "Huddersfield", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Sheffield Weds", AwayTeam = "Preston", Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Stoke", AwayTeam = "Watford", Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Sunderland", AwayTeam = "Rotherham", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Swansea", AwayTeam = "Coventry", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Norwich", AwayTeam = "Millwall", Date = "20/08/2023", FTHG = 3, FTAG = 1 },
            
            new() { HomeTeam = "Sociedad", AwayTeam = "Celta", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Almeria", AwayTeam = "Real Madrid", Date = "19/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Osasuna", AwayTeam = "Ath Bilbao", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Girona", AwayTeam = "Getafe", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Barcelona", AwayTeam = "Cadiz", Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Betis", AwayTeam = "Ath Madrid", Date = "20/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Granada", AwayTeam = "Vallecano", Date = "21/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Alaves", AwayTeam = "Sevilla", Date = "21/08/2023", FTHG = 4, FTAG = 3 },
            new() { HomeTeam = "Eibar", AwayTeam = "Elche", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Espanol", AwayTeam = "Santander", Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Levante", AwayTeam = "Burgos", Date = "19/08/2023", FTHG = 3, FTAG = 2 },
            new() { HomeTeam = "Alcorcon", AwayTeam = "Leganes", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Oviedo", AwayTeam = "Ferrol", Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Sp Gijon", AwayTeam = "Mirandes", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Albacete", AwayTeam = "Amorebieta", Date = "20/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Villarreal B", AwayTeam = "Eldense", Date = "21/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = "Huesca", AwayTeam = "Tenerife", Date = "21/08/2023", FTHG = 0, FTAG = 2 },
            
            new() { HomeTeam = "Cosenza", AwayTeam = "Ascoli", Date = "19/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Cremonese", AwayTeam = "Catanzaro", Date = "19/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Ternana", AwayTeam = "Sampdoria", Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Sudtirol", AwayTeam = "Spezia", Date = "20/08/2023", FTHG = 3, FTAG = 3 },
            new() { HomeTeam = "Cittadella", AwayTeam = "Reggiana", Date = "20/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Parma", AwayTeam = "FeralpiSalo", Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Venezia", AwayTeam = "Como", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Empoli", AwayTeam = "Verona", Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Frosinone", AwayTeam = "Napoli", Date = "19/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Genoa", AwayTeam = "Fiorentina", Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = "Inter", AwayTeam = "Monza", Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Roma", AwayTeam = "Salernitana", Date = "20/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Sassuolo", AwayTeam = "Atalanta", Date = "20/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Lecce", AwayTeam = "Lazio", Date = "20/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Udinese", AwayTeam = "Juventus", Date = "20/08/2023", FTHG = 0, FTAG = 3 },
            
            new() { HomeTeam = "Lyon", AwayTeam = "Montpellier", Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = "Toulouse", AwayTeam = "Paris SG", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Lille", AwayTeam = "Nantes", Date = "30/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Le Havre", AwayTeam = "Brest", Date = "20/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Lorient", AwayTeam = "Nice", Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Reims", AwayTeam = "Clermont", Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Monaco", AwayTeam = "Strasbourg", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Lens", AwayTeam = "Rennes", Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            
            new() { HomeTeam = "Angers", AwayTeam = "Auxerre", Date = "19/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Amiens", AwayTeam = "Bastia", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Annecy", AwayTeam = "Dunkerque", Date = "19/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Concarneau", AwayTeam = "Caen", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Grenoble", AwayTeam = "Troyes", Date = "19/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Laval", AwayTeam = "Rodez", Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Pau FC", AwayTeam = "Paris FC", Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "St Etienne", AwayTeam = "Quevilly Rouen", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Valenciennes", AwayTeam = "Guingamp", Date = "19/08/2023", FTHG = 0, FTAG = 0 }
        };

        // ACTUAL ASSERT
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals ||
                            lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals ||
                            lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals ||
                            lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3 && actual.Type == BetType.TwoToThreeGoals;

            if (actual.Qualified)
            {
                if (isCorrect)
                {
                    correctCount++;
                }
                else
                {
                    wrongCount++;
                    _testOutputHelper.WriteLine($" ----  {lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} ---- ");
                }
                totalCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg}");
            }
        }
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount}");
    }
    
       [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void Test_25_27_August_Prediction()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = "Blackburn", AwayTeam = "Hull", Date = "19/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Hamburg", AwayTeam = "Hertha", Date = "19/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Fulham", AwayTeam = "Brentford", Date = "19/08/2023", FTHG = 0, FTAG = 3 },
            new() { HomeTeam = "Wolves", AwayTeam = "Brighton", Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = "Man City", AwayTeam = "Newcastle", Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Aston Villa", AwayTeam = "Everton", Date = "20/08/2023", FTHG = 4, FTAG = 0 },
            new() { HomeTeam = "Plymouth", AwayTeam = "Southampton", Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Bristol City", AwayTeam = "Birmingham", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Leicester", AwayTeam = "Cardiff", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Middlesbrough", AwayTeam = "Huddersfield", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Sheffield Weds", AwayTeam = "Preston", Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Stoke", AwayTeam = "Watford", Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Sunderland", AwayTeam = "Rotherham", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Swansea", AwayTeam = "Coventry", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Norwich", AwayTeam = "Millwall", Date = "20/08/2023", FTHG = 3, FTAG = 1 },
            
            new() { HomeTeam = "Sociedad", AwayTeam = "Celta", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Almeria", AwayTeam = "Real Madrid", Date = "19/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Osasuna", AwayTeam = "Ath Bilbao", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Girona", AwayTeam = "Getafe", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Barcelona", AwayTeam = "Cadiz", Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Betis", AwayTeam = "Ath Madrid", Date = "20/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Granada", AwayTeam = "Vallecano", Date = "21/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Alaves", AwayTeam = "Sevilla", Date = "21/08/2023", FTHG = 4, FTAG = 3 },
            new() { HomeTeam = "Eibar", AwayTeam = "Elche", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Espanol", AwayTeam = "Santander", Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Levante", AwayTeam = "Burgos", Date = "19/08/2023", FTHG = 3, FTAG = 2 },
            new() { HomeTeam = "Alcorcon", AwayTeam = "Leganes", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Oviedo", AwayTeam = "Ferrol", Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Sp Gijon", AwayTeam = "Mirandes", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Albacete", AwayTeam = "Amorebieta", Date = "20/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Villarreal B", AwayTeam = "Eldense", Date = "21/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = "Huesca", AwayTeam = "Tenerife", Date = "21/08/2023", FTHG = 0, FTAG = 2 },
            
            new() { HomeTeam = "Cosenza", AwayTeam = "Ascoli", Date = "19/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Cremonese", AwayTeam = "Catanzaro", Date = "19/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Ternana", AwayTeam = "Sampdoria", Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Sudtirol", AwayTeam = "Spezia", Date = "20/08/2023", FTHG = 3, FTAG = 3 },
            new() { HomeTeam = "Cittadella", AwayTeam = "Reggiana", Date = "20/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Parma", AwayTeam = "FeralpiSalo", Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Venezia", AwayTeam = "Como", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Empoli", AwayTeam = "Verona", Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Frosinone", AwayTeam = "Napoli", Date = "19/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Genoa", AwayTeam = "Fiorentina", Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = "Inter", AwayTeam = "Monza", Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Roma", AwayTeam = "Salernitana", Date = "20/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Sassuolo", AwayTeam = "Atalanta", Date = "20/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Lecce", AwayTeam = "Lazio", Date = "20/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Udinese", AwayTeam = "Juventus", Date = "20/08/2023", FTHG = 0, FTAG = 3 },
            
            new() { HomeTeam = "Lyon", AwayTeam = "Montpellier", Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = "Toulouse", AwayTeam = "Paris SG", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Lille", AwayTeam = "Nantes", Date = "30/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Le Havre", AwayTeam = "Brest", Date = "20/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Lorient", AwayTeam = "Nice", Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Reims", AwayTeam = "Clermont", Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Monaco", AwayTeam = "Strasbourg", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Lens", AwayTeam = "Rennes", Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            
            new() { HomeTeam = "Angers", AwayTeam = "Auxerre", Date = "19/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Amiens", AwayTeam = "Bastia", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Annecy", AwayTeam = "Dunkerque", Date = "19/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Concarneau", AwayTeam = "Caen", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Grenoble", AwayTeam = "Troyes", Date = "19/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Laval", AwayTeam = "Rodez", Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Pau FC", AwayTeam = "Paris FC", Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "St Etienne", AwayTeam = "Quevilly Rouen", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Valenciennes", AwayTeam = "Guingamp", Date = "19/08/2023", FTHG = 0, FTAG = 0 }
        };

        // ACTUAL ASSERT
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals ||
                            lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals ||
                            lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals ||
                            lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3 && actual.Type == BetType.TwoToThreeGoals;

            if (actual.Qualified)
            {
                if (isCorrect)
                {
                    correctCount++;
                }
                else
                {
                    wrongCount++;
                    _testOutputHelper.WriteLine($" ----  {lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} ---- ");
                }
                totalCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg}");
            }
        }
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount}");
    }
    
    
        
    [Fact, Description("Championship first game day")]
    public void Championship_First_Game_Day()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = Championship.SheffieldWeds, AwayTeam = Championship.Southampton, Date = "04/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = Championship.Blackburn, AwayTeam = Championship.WestBrom, Date = "05/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = Championship.Bristol, AwayTeam = Championship.Preston, Date = "05/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = Championship.Middlesbrough, AwayTeam = Championship.Millwall, Date = "05/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = Championship.Norwich, AwayTeam = Championship.Hull, Date = "05/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = Championship.Plymouth, AwayTeam = Championship.Huddersfield, Date = "05/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = Championship.Stoke, AwayTeam = Championship.Rotherham, Date = "05/08/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = Championship.Swansea, AwayTeam = Championship.Birmingham, Date = "05/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = Championship.Watford, AwayTeam = Championship.QPR, Date = "05/08/2023", FTHG = 4, FTAG = 0 },
            new() { HomeTeam = Championship.Leicester, AwayTeam = Championship.Coventry, Date = "06/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = Championship.Leeds, AwayTeam = Championship.Cardiff, Date = "06/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = Championship.Sunderland, AwayTeam = Championship.Ipswich, Date = "06/08/2023", FTHG = 1, FTAG = 2 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ Championship first day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = (lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals) ||
                            (lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals) ||
                            (lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals) ||
                            ((lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3) &&
                             actual.Type == BetType.TwoToThreeGoals) ||
                            (lastSixGame.FTAG > lastSixGame.FTHG && actual.Type == BetType.AwayWin) ||
                            (lastSixGame.FTHG > lastSixGame.FTAG && actual.Type == BetType.HomeWin);

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
            totalCount++;
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");
    }
            
    
    [Fact, Description("Championship second game day")]
    public void Championship_Second_Game_Day()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = Championship.Coventry, AwayTeam = Championship.Middlesbrough, Date = "12/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = Championship.Birmingham, AwayTeam = Championship.Leeds, Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = Championship.Cardiff, AwayTeam = Championship.QPR, Date = "12/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = Championship.Huddersfield, AwayTeam = Championship.Leicester, Date = "12/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = Championship.Hull, AwayTeam = Championship.SheffieldWeds, Date = "12/08/2023", FTHG = 4, FTAG = 2 },
            new() { HomeTeam = Championship.Ipswich, AwayTeam = Championship.Stoke, Date = "12/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = Championship.Millwall, AwayTeam = Championship.Bristol, Date = "12/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = Championship.Preston, AwayTeam = Championship.Sunderland, Date = "12/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = Championship.Rotherham, AwayTeam = Championship.Blackburn, Date = "12/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = Championship.Southampton, AwayTeam = Championship.Norwich, Date = "12/08/2023", FTHG = 4, FTAG = 4 },
            new() { HomeTeam = Championship.Watford, AwayTeam = Championship.Plymouth, Date = "12/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = Championship.WestBrom, AwayTeam = Championship.Swansea, Date = "12/08/2023", FTHG = 3, FTAG = 2 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ Championship second day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = (lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals) ||
                            (lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals) ||
                            (lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals) ||
                            ((lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3) &&
                             actual.Type == BetType.TwoToThreeGoals) ||
                            (lastSixGame.FTAG > lastSixGame.FTHG && actual.Type == BetType.AwayWin) ||
                            (lastSixGame.FTHG > lastSixGame.FTAG && actual.Type == BetType.HomeWin);

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
            totalCount++;
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");
    }
    
        
    [Fact, Description("Championship third game day")]
    public void Championship_Third_Game_Day()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = Championship.Leeds, AwayTeam = Championship.WestBrom, Date = "18/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = Championship.Plymouth, AwayTeam = Championship.Southampton, Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = Championship.Blackburn, AwayTeam = Championship.Hull, Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = Championship.Bristol, AwayTeam = Championship.Birmingham, Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = Championship.Leicester, AwayTeam = Championship.Cardiff, Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = Championship.Middlesbrough, AwayTeam = Championship.Huddersfield, Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = Championship.QPR, AwayTeam = Championship.Ipswich, Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = Championship.SheffieldWeds, AwayTeam = Championship.Preston, Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = Championship.Stoke, AwayTeam = Championship.Watford, Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = Championship.Sunderland, AwayTeam = Championship.Rotherham, Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = Championship.Swansea, AwayTeam = Championship.Coventry, Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = Championship.Norwich, AwayTeam = Championship.Millwall, Date = "20/08/2023", FTHG = 3, FTAG = 1 },
        };

        // ACTUAL ASSERT
        _testOutputHelper.WriteLine(" ------------------ Championship third day ------------------- ");
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = (lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals) ||
                            (lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals) ||
                            (lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals) ||
                            ((lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3) &&
                             actual.Type == BetType.TwoToThreeGoals) ||
                            (lastSixGame.FTAG > lastSixGame.FTHG && actual.Type == BetType.AwayWin) ||
                            (lastSixGame.FTHG > lastSixGame.FTAG && actual.Type == BetType.HomeWin);

            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                correctCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ✅ - ");
            }
            else
            {
                wrongCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg} - ❌ - ");
            }
            totalCount++;
        }
        
        var accuracyRate = correctCount / (double)totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount} accuracy rate: {accuracyRate:F}");
    }
    
}
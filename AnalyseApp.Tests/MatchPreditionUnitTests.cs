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

    private int _totalCount;
    private int _correctCount;
    private int _wrongCount;

    public PremierLeagueUnitTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions
        {
            RawCsvDir = "/Users/shivm/Documents/projects/AnalyseApp/data/raw_csv",
            Upcoming = "/Users/shivm/Documents/projects/AnalyseApp/data/upcoming"
        };

        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        _fileProcessor = new FileProcessor(optionsWrapper);
        
        _matchPredictor = new MatchPredictor(_fileProcessor, new DataService(_fileProcessor));
        _testOutputHelper = testOutputHelper;
    }

    [
        Theory(DisplayName = "Premier league predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),

    ]
    public void PremierLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.Div == "E0")
            .ToList();

        if (premierLeagueMatches.Count is 0) return;
        
        // ACTUAL 
        foreach (var matches in premierLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(75);
    }
    
    [
        Theory(DisplayName = "championship league predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),

    ]
    public void ChampionshipLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.Div == "E1")
            .ToList();

        if (premierLeagueMatches.Count is 0) return;
        
        // ACTUAL 
        foreach (var matches in premierLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(75);
    }
    
    [
        Theory(DisplayName = "League one predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),

    ]
    public void LeagueOne_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.Div == "E2")
            .ToList();

        if (premierLeagueMatches.Count is 0) return;
        
        // ACTUAL 
        foreach (var matches in premierLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(75);
    }

    [
        Theory(DisplayName = "Bundesliga predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),

    ]
    public void Bundesliga_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.Div == "D1")
            .ToList();

        if (premierLeagueMatches.Count is 0) return;
        
        // ACTUAL 
        foreach (var matches in premierLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(75);
    }

    
    [
        Theory(DisplayName = "Bundesliga 2 predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),

    ]
    public void Bundesliga2_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.Div == "D2")
            .ToList();

        if (premierLeagueMatches.Count is 0) return;
        
        // ACTUAL 
        foreach (var matches in premierLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(75);
    }

    
    [
        Theory(DisplayName = "Spanish league predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),
    ]
    public void SpanishLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var spanishLeagueMatches = fixture
            .Where(i => i.Div == "SP1")
            .ToList();

        if (spanishLeagueMatches.Count is 0) return;

        // ACTUAL 
        foreach (var matches in spanishLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(70);
    }
    
    [
        Theory(DisplayName = "Italian league predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),
    ]
    public void ItalianLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var spanishLeagueMatches = fixture
            .Where(i => i.Div == "I1")
            .ToList();

        if (spanishLeagueMatches.Count is 0) return;

        // ACTUAL 
        foreach (var matches in spanishLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(70);
    }
    
    [
        Theory(DisplayName = "French league predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),
    ]
    public void FrenchLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var frenchGames = fixture
            .Where(i => i.Div == "F1")
            .ToList();

        if (frenchGames.Count is 0) return;

        // ACTUAL 
        foreach (var matches in frenchGames)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(70);
    }

    [
        Theory(DisplayName = "Spanish league two predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),
    ]
    public void SpanishLeagueTwo_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var spanishLeagueMatches = fixture
            .Where(i => i.Div == "SP2")
            .ToList();

        if (spanishLeagueMatches.Count is 0) return;

        // ACTUAL 
        foreach (var matches in spanishLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(70);
    }

    [
        Theory(DisplayName = "Italian league two predictions"), 
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),
    ]
    public void ItalianLeagueTwo_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var spanishLeagueMatches = fixture
            .Where(i => i.Div == "I2")
            .ToList();

        if (spanishLeagueMatches.Count is 0) return;

        // ACTUAL 
        foreach (var matches in spanishLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(70);
    }
    
    [
        Theory(DisplayName = "French league two predictions"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-21-10"),
    ]
    public void FrenchLeagueTwo_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var frenchGames = fixture
            .Where(i => i.Div == "F2")
            .ToList();

        if (frenchGames.Count is 0) return;

        // ACTUAL 
        foreach (var matches in frenchGames)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.Msg}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.Msg}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(70);
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
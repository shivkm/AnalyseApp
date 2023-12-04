using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.Options;
using AnalyseApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;
using Match = AnalyseApp.models.Match;

namespace AnalyseApp.Tests;

public class PremierLeagueUnitTests
{
    private readonly IFileProcessor _fileProcessor;
    private readonly IMatchPredictor _matchPredictor;
    private readonly ITestOutputHelper _testOutputHelper;

    private const int PassingPercentage = 80;
    private int _totalCount;
    private int _correctCount;
    private int _wrongCount;

    public PremierLeagueUnitTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions
        {
            RawCsvDir = "/Users/shivm/Workspace/AnalyseApp/data/raw_csv",
            Upcoming = "/Users/shivm/Workspace/AnalyseApp/data/upcoming",
            MachineLearningModel = "/Users/shivm/Workspace/AnalyseApp/data/ml_model",
        };

        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        _fileProcessor = new FileProcessor(optionsWrapper);
        _matchPredictor = new MatchPredictor(_fileProcessor, new MachineLearning(), optionsWrapper);
        _testOutputHelper = testOutputHelper;
    }

    public PremierLeagueUnitTests(IFileProcessor fileProcessor)
    {
        _fileProcessor = fileProcessor;
    }

    [
        Theory(DisplayName = "Premier league predictions"), 
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")

    ]
    public void PremierLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.League == "E0")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ -  {matches.FullTimeHomeGoals}:{matches.FullTimeAwayGoals}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ -  {matches.FullTimeHomeGoals}:{matches.FullTimeAwayGoals}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(PassingPercentage);
    }
    
    [
        Theory(DisplayName = "championship league predictions"), 
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")

    ]
    public void ChampionshipLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.League == "E1")
            .ToList();

        if (premierLeagueMatches.Count is 0) return;
        
        // ACTUAL 
        foreach (var matches in premierLeagueMatches)
        {
            var actual = _matchPredictor.Execute(matches);
            var isCorrect = GetTheCorrectResult(matches, actual.Type);
            
            var msg = $"{matches.Date} - {matches.HomeTeam}:{matches.AwayTeam} {actual.Type}, {actual.Percentage:F} ";
            if (!actual.Qualified) continue;
            
            if (isCorrect)
            {
                _correctCount++;
                _testOutputHelper.WriteLine($"{msg} - ✅ - {matches.FullTimeHomeGoals}:{matches.FullTimeAwayGoals}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {matches.FullTimeHomeGoals}:{matches.FullTimeAwayGoals}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(PassingPercentage);
    }
    
    [
        Theory(DisplayName = "League one predictions"), 
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")

    ]
    public void LeagueOne_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.League == "E2")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.HomeScore}:{actual.AwayScore}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.HomeScore}:{actual.AwayScore}");
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
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")

    ]
    public void Bundesliga_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.League == "D1")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.HomeScore}:{actual.AwayScore}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.HomeScore}:{actual.AwayScore}");
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
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")

    ]
    public void Bundesliga2_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var premierLeagueMatches = fixture
            .Where(i => i.League == "D2")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.HomeScore}:{actual.AwayScore}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.HomeScore}:{actual.AwayScore}");
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
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")
    ]
    public void SpanishLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var spanishLeagueMatches = fixture
            .Where(i => i.League == "SP1")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.HomeScore}:{actual.AwayScore}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.HomeScore}:{actual.AwayScore}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(PassingPercentage);
    }
    
    [
        Theory(DisplayName = "Italian league predictions"), 
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")
    ]
    public void ItalianLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var spanishLeagueMatches = fixture
            .Where(i => i.League == "I1")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.HomeScore}:{actual.AwayScore}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.HomeScore}:{actual.AwayScore}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(PassingPercentage);
    }
    
    [
        Theory(DisplayName = "French league predictions"), 
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")
    ]
    public void FrenchLeague_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var frenchGames = fixture
            .Where(i => i.League == "F1")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.HomeScore}:{actual.AwayScore}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.HomeScore}:{actual.AwayScore}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(PassingPercentage);
    }

    [
        Theory(DisplayName = "Spanish league two predictions"), 
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")
    ]
    public void SpanishLeagueTwo_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var spanishLeagueMatches = fixture
            .Where(i => i.League == "SP2")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.HomeScore}:{actual.AwayScore}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.HomeScore}:{actual.AwayScore}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(PassingPercentage);
    }

    [
        Theory(DisplayName = "Italian league two predictions"), 
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")
    ]
    public void ItalianLeagueTwo_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var spanishLeagueMatches = fixture
            .Where(i => i.League == "I2")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.HomeScore}:{actual.AwayScore}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.HomeScore}:{actual.AwayScore}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(PassingPercentage);
    }
    
    [
        Theory(DisplayName = "French league two predictions"),
        InlineData("fixture-11-8"),
        InlineData("fixture-18-8"),
        InlineData("fixture-25-8"),
        InlineData("fixture-1-9"),
        InlineData("fixture-15-9"),
        InlineData("fixture-22-9"),
        InlineData("fixture-29-9"),
        InlineData("fixture-6-10"),
        InlineData("fixture-20-10"),
        InlineData("fixture-27-10"),
        InlineData("fixture-3-11"),
        InlineData("fixture-10-11"),
        InlineData("fixture-24-11")
    ]
    public void FrenchLeagueTwo_Prediction_TheAccuracyRate_ShouldBeEqualOrGreaterThan_80Percent(string fixtureName)
    {
        // ARRANGE
        var fixture = _fileProcessor.GetUpcomingGamesBy(fixtureName);
        var frenchGames = fixture
            .Where(i => i.League == "F2")
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
                _testOutputHelper.WriteLine($"{msg} - ✅ - {actual.HomeScore}:{actual.AwayScore}");
            }
            else
            {
                _wrongCount++;
                _testOutputHelper.WriteLine($"{msg} - ❌ - {actual.HomeScore}:{actual.AwayScore}");
            }
            _totalCount++;
        }

        var accuracyRate = _correctCount / (double)_totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");

        // ASSERT
        accuracyRate.Should().BeGreaterOrEqualTo(PassingPercentage);
    }

    private static bool GetTheCorrectResult(Match match, BetType betType)
    {
        return betType switch
        {
            BetType.OverTwoGoals when match.FullTimeHomeGoals + match.FullTimeAwayGoals > 2 => true,
            BetType.GoalGoal when match is { FullTimeHomeGoals: > 0, FullTimeAwayGoals: > 0 } => true,
            BetType.UnderThreeGoals when match.FullTimeAwayGoals + match.FullTimeHomeGoals < 3 => true,
            BetType.TwoToThreeGoals when match.FullTimeAwayGoals + match.FullTimeHomeGoals is 2 or 3 => true,
            BetType.HomeWin when match.FullTimeHomeGoals > match.FullTimeAwayGoals => true,
            BetType.AwayWin when match.FullTimeHomeGoals < match.FullTimeAwayGoals => true,
            _ => false
        };
    }
}
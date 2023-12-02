using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Models;
using AnalyseApp.Options;
using AnalyseApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace AnalyseApp.Tests;

public class MatchPredictionTests
{
    private readonly IFileProcessor _fileProcessor;
    private readonly IMatchPredictor _matchPredictor;
    private readonly ITestOutputHelper _testOutputHelper;
    
    
    private const int passingPercentage = 80;
    private int _totalCount;
    private int _correctCount;
    private int _wrongCount;
    
    public MatchPredictionTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions
        {
            RawCsvDir = "/Users/shivm/Workspace/AnalyseApp/data/raw_csv",
            Upcoming = "/Users/shivm/Workspace/AnalyseApp/data/upcoming"
        };

        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        _fileProcessor = new FileProcessor(optionsWrapper);
        _matchPredictor = new MatchPredictor(_fileProcessor, new MachineLearning());
        _testOutputHelper = testOutputHelper;
    }

    [
        Theory(DisplayName = "Generate ticket predictions"), 
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
    public void GivenSomePredictions_WhenGenerateTicketExecuted_ThenTheTicketShouldHaveAllPredictionsCorrect(string fixture)
    {
        var tickets = _matchPredictor.GenerateTicketBy(4, 3, BetType.GoalGoal, fixture);
        foreach (var ticket in tickets)
        {
            foreach (var prediction in ticket.Predictions)
            {
                var isCorrect = GetTheCorrectResult(prediction, prediction.Type);
                if (isCorrect)
                {
                    _correctCount++;
                    _testOutputHelper.WriteLine($"{prediction.Msg} - ✅ -  {prediction.HomeScore}:{prediction.AwayScore}");
                }
                else
                {
                    _wrongCount++;
                    _testOutputHelper.WriteLine($"{prediction.Msg} - ❌ -  {prediction.HomeScore}:{prediction.AwayScore}");
                }
                _totalCount++;
            }
           
            var accuracyRate = _correctCount / (double)_totalCount * 100;
            _testOutputHelper.WriteLine($"Count: {_totalCount}, correct count: {_correctCount}, wrong count: {_wrongCount}  ");
            
            // ASSERT
            accuracyRate.Should().BeGreaterOrEqualTo(passingPercentage);
        }
    }
    
    private static bool GetTheCorrectResult(Prediction match, BetType betType)
    {
        return betType switch
        {
            BetType.OverTwoGoals when match.HomeScore + match.AwayScore > 2 => true,
            BetType.GoalGoal when match is { HomeScore: > 0, AwayScore: > 0 } => true,
            BetType.UnderThreeGoals when match.AwayScore + match.HomeScore < 3 => true,
            BetType.TwoToThreeGoals when match.AwayScore + match.HomeScore is 2 or 3 => true,
            BetType.HomeWin when match.HomeScore > match.AwayScore => true,
            BetType.AwayWin when match.HomeScore < match.AwayScore => true,
            _ => false
        };
    }
}
using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Options;
using AnalyseApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace AnalyseApp.Tests;

public class PredictionServiceTests
{
    private readonly IFileProcessor _fileProcessor;
    private readonly IPredictionService _predictionService;
    private readonly ITestOutputHelper _testOutputHelper;
    
    
    private int _totalCount;
    private int _correctCount;
    private int _wrongCount;
    
    public PredictionServiceTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions
        {
            RawCsvDir = "/Users/shivm/Workspace/AnalyseApp/data/raw_csv",
            Upcoming = "/Users/shivm/Workspace/AnalyseApp/data/upcoming",
            MachineLearningModel = "/Users/shivm/Workspace/AnalyseApp/data/ml_model",
        };

        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        _fileProcessor = new FileProcessor(optionsWrapper);
        _predictionService = new PredictionService(_fileProcessor, new MachineLearningEngine(), optionsWrapper);
        _testOutputHelper = testOutputHelper;
    }

    [
        Theory(DisplayName = "Get randomly predicted games for ticket"), 
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
        InlineData("fixture-24-11"),
        InlineData("fixture-1-12"),
        InlineData("fixtures.csv"),

    ]
    public void GivenSomePredictions_WhenGenerateTicketExecuted_ThenTheTicketShouldHaveAllPredictionsCorrect(string fixture)
    {
        // Arrange
        const int gameCount = 22;
        const double expectedAccuracy = 90.0; 
        
        // Act
        var predictions = _predictionService.GenerateRandomPredictionsBy(gameCount, fixture);
    
        if (predictions.Count == 0)
        {
            _testOutputHelper.WriteLine($"No predictions generated for {fixture}");
            return;
        }

        foreach (var prediction in predictions) UpdatePredictionCountsAndLogResult(prediction);
            
        _testOutputHelper.WriteLine(
            $"Count: {_totalCount},  correct count: {_correctCount}, wrong count: {_wrongCount}");
           
        var actualAccuracy = CalculateAccuracyRate();
        // Assert
        actualAccuracy.Should().BeGreaterOrEqualTo(
            expectedAccuracy, 
            $"Accuracy for {fixture} should meet expected threshold."
            );
    }
    
    private double CalculateAccuracyRate() => _correctCount / (double)_totalCount * 100;
  
    private void UpdatePredictionCountsAndLogResult(Prediction prediction)
    {
        var isPredictionCorrect = IsPredictionCorrect(prediction, prediction.Type);
        UpdateCounts(isPredictionCorrect);
        LogPredictionResult(prediction, isPredictionCorrect);
    }

    private void UpdateCounts(bool isPredictionCorrect)
    {
        if (isPredictionCorrect)
        {
            _correctCount++;
        }
        else
        {
            _wrongCount++;
        }
        _totalCount++;
    }

    private void LogPredictionResult(Prediction prediction, bool isPredictionCorrect)
    {
        var resultIcon = isPredictionCorrect ? "✅" : "❌";
        _testOutputHelper.WriteLine(
            $" {prediction.Msg} {prediction.Type} - {resultIcon} - {prediction.HomeScore}:{prediction.AwayScore} ");
    }
    
    private static bool IsPredictionCorrect(Prediction prediction, BetType betType)
    {
        return betType switch
        {
            BetType.OverTwoGoals => prediction.HomeScore + prediction.AwayScore > 2,
            BetType.GoalGoal => prediction is { HomeScore: > 0, AwayScore: > 0 },
            BetType.UnderThreeGoals => prediction.HomeScore + prediction.AwayScore < 3,
            BetType.TwoToThreeGoals => prediction.HomeScore + prediction.AwayScore is 2 or 3,
            BetType.HomeWin => prediction.HomeScore > prediction.AwayScore,
            BetType.AwayWin => prediction.HomeScore < prediction.AwayScore,
            _ => false,
        };
    }


}
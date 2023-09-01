using AnalyseApp.Interfaces;
using AnalyseApp.Options;
using AnalyseApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace AnalyseApp.Tests;

public class MatchPredictionUnitTests
{
    private readonly IMatchPredictor _matchPredictor;

    public MatchPredictionUnitTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions 
        {
            RawCsvDir = "C:\\shivm\\AnalyseApp\\data\\raw_csv",
            Upcoming = "C:\\shivm\\AnalyseApp\\data\\upcoming"
        };
        
        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        var fileProcessor = new FileProcessor(optionsWrapper);
        
        _matchPredictor = new MatchPredictor(fileProcessor, new PoissonService(), new DataService(fileProcessor));
    }
    
    [Fact]
    public void Given_Matches_From_18_08_To_21_08_When_Matches_Prediction_Executed_Then_The_Accuracy_Rate_Over_80_Percent()
    {
        // ACTUAL
        var accuracyRate = _matchPredictor.GetPredictionAccuracyRate("fixture-18-8");

        // ASSERT
        accuracyRate.Should().BeGreaterThan(80);
    }
    
    [Fact]
    public void Given_Matches_From_25_08_To_28_08_When_Matches_Prediction_Executed_Then_The_Accuracy_Rate_Over_80_Percent()
    {
        // ACTUAL
        var accuracyRate = _matchPredictor.GetPredictionAccuracyRate("fixture-25-8");

        // ASSERT
        accuracyRate.Should().BeGreaterThan(80);
    }
}
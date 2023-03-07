using PredictionTool.Extensions;
using PredictionTool.Interfaces;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class AnalyseService: IAnalyseService
{
    private readonly IFileProcessor _fileProcessor;
    private readonly ICalculatorService _calculatorService;
    private readonly IFilterService _filterService;

    public AnalyseService(
        IFileProcessor fileProcessor, IFilterService filterService,
        ICalculatorService calculatorService)
    {
        _fileProcessor = fileProcessor;
        _filterService = filterService;
        _calculatorService = calculatorService;
    }

    public async Task StartAnalyseAsync()
    {
        // Change this to load data backward for test
        var historicalEnd = new DateTime(2023, 02, 22);
        var endDate = DateTime.Now.AddDays(5);
        //await _fileProcessor.CreateUpcomingFixtureBy(default);
        //await _fileProcessor.CreateHistoricalGamesFile(default);
        var historicalGames = _fileProcessor.GetHistoricalGamesBy(historicalEnd);
        var upcomingGames = _fileProcessor.GetUpcomingGamesBy(endDate);
        var result = new List<QualifiedGames>();

        foreach (var upcomingGame in upcomingGames)
        {
            var qualifiedGame = new QualifiedGames(
                upcomingGame.DateTime,
                upcomingGame.Home,
                upcomingGame.Away,
                upcomingGame.League
            );
            
            var gameProbabilities = _calculatorService.Calculate(
                historicalGames,
                upcomingGame.Home,
                upcomingGame.Away,
                upcomingGame.League
            );

            var qualified = _filterService.FilterGames(
                qualifiedGame,
                gameProbabilities,
                historicalGames
            );

            result.Add(qualifiedGame with
            {
                Key = qualified.Key,
                Probability = qualified.Probability
            });
        }
        
        Console.WriteLine($"count: {result.Count}");
        result.ForEach(i => Console.WriteLine($"{i}\t"));
    }

}
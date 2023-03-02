using PredictionTool.Interfaces;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class AnalyseService: IAnalyseService
{
    private readonly IFileProcessor _fileProcessor;
    private readonly ITeamStrengthCalculator _teamStrengthCalculator;
    private readonly IFilterService _filterService;

    public AnalyseService(
        IFileProcessor fileProcessor, IFilterService filterService, ITeamStrengthCalculator teamStrengthCalculator)
    {
        _fileProcessor = fileProcessor;
        _filterService = filterService;
        _teamStrengthCalculator = teamStrengthCalculator;
    }

    public async  Task StartAnalyseAsync()
    {
        // Change this to load data backward for test
        var historicalEnd = new DateTime(2023, 02, 16);
        var endDate = DateTime.Now;
        //await _fileProcessor.CreateUpcomingFixtureBy(default);
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
            
            var gameProbabilities = _teamStrengthCalculator.Calculate(
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

            if (qualified.Probability == 0)
                continue;

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
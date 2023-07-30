using AnalyseApp.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AnalyseApp;

public class Worker: BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var analyseService = scope.ServiceProvider.GetRequiredService<IAnalyseService>();
                var predictService = scope.ServiceProvider.GetRequiredService<IPredictService>();
                var overUnderPredictor = scope.ServiceProvider.GetRequiredService<IOverUnderPredictor>();

                //overUnderPredictor.CreateFiles();
                // analyseService.CalculationAnalysis();
                 predictService.OverUnderPredictor("", "");
                 predictService.TeamAnalysisBy();
                // will be called only if the season finish or start
                //await fileProcessor.CreateHistoricalGamesFile(stoppingToken);

                // Change the match day in each league and than create upcoming matches fixtures
                //await fileProcessor.CreateUpcomingFixtureBy(stoppingToken);
            }
            catch (HttpRequestException ex)
            {
                Console.Write(ex);
            }
        }
    }
}

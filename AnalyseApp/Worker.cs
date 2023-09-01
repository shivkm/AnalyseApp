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
                var predictService = scope.ServiceProvider.GetRequiredService<IMatchPredictor>();
                predictService.GetPredictionAccuracyRate("");
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

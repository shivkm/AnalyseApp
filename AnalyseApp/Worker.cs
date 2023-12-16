using AnalyseApp.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AnalyseApp;

public class Worker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var predictService = scope.ServiceProvider.GetRequiredService<IPredictionService>();
                var footballService = scope.ServiceProvider.GetRequiredService<IFootballService>();
                //predictService.GenerateFixtureFiles("");
                //predictService.GenerateRandomPredictionsBy();

             //   await footballService.QueryAndSaveLeaguesBy();
            }
            catch (HttpRequestException ex)
            {
                Console.Write(ex);
            }
        }
    }
}

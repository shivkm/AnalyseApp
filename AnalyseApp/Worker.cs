using AnalyseApp.Enums;
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
                //predictService.GenerateFixtureFiles("");
                predictService.GenerateTicketBy(3, 1, BetType.OverTwoGoals, "fixtures.csv");
            }
            catch (HttpRequestException ex)
            {
                Console.Write(ex);
            }
        }
    }
}

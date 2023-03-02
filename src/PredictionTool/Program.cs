using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PredictionTool;
using PredictionTool.Interfaces;
using PredictionTool.Options;
using PredictionTool.Services;

IConfigurationRoot? configurationRoot = null;

// allows serilog to find the config
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, builder) =>
    {
        builder.AddJsonFile("appsettings.json");
        configurationRoot = builder.Build();
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddOptions<FileProcessorOptions>()
            .Configure(o => configurationRoot?.GetSection("FileProcessor").Bind(o));
        
        var baseUrl = configurationRoot?["FootballApi:BaseUrl"];
        var token = configurationRoot?["FootballApi:Token"];
        
        services.AddHttpClient<IFootballApi, FootballApi>(ctx =>
        {
            ctx.BaseAddress = new Uri(baseUrl);
            ctx.DefaultRequestHeaders.Add("X-Auth-Token", token);
        });
        services.AddScoped<IFileProcessor, FileProcessor>();
        services.AddScoped<ITeamStrengthCalculator, TeamStrengthCalculator>();
        services.AddScoped<IFilterService, FilterService>();
        services.AddScoped<IAnalyseService, AnalyseService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();

[ExcludeFromCodeCoverage]
public partial class Program
{
}
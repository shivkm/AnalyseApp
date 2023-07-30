using System.Diagnostics.CodeAnalysis;
using AnalyseApp;
using AnalyseApp.Interfaces;
using AnalyseApp.Options;
using AnalyseApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        
        services.AddScoped<IFileProcessor, FileProcessor>();
        services.AddScoped<IAnalyseService, AnalyseService>();
        services.AddScoped<IPredictService, PredictService>();
        services.AddScoped<IOverUnderPredictor, OverUnderPredictor>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();

[ExcludeFromCodeCoverage]
public partial class Program { }
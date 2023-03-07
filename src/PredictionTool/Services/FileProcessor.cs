using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using PredictionTool.Enums;
using PredictionTool.Extensions;
using PredictionTool.Interfaces;
using PredictionTool.Models;
using PredictionTool.Options;

namespace PredictionTool.Services;

public class FileProcessor : IFileProcessor
{
    private readonly FileProcessorOptions _options;
    private readonly IFootballApi _footballApi;
    
    private readonly List<Game> _historicalGames = new();

    public FileProcessor(IFootballApi footballApi, IOptions<FileProcessorOptions> options)
    {
        _options = options.Value;
        _footballApi = footballApi;
    }

    public async Task CreateHistoricalGamesFile(CancellationToken token)
    {
        var files = Directory.GetFiles(_options.RawCsvDir);
        
        foreach (var file in files)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower()
            };
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, config);

            var currentFileGames = csv.GetRecords<HistoricalGame>()
                .Select(m => new Game
            {
                Home = m.HomeTeam,
                Away = m.AwayTeam,
                League = m.Div,
                DateTime = DateTime.Parse(m.Date),
                FullTimeResult = m.FTR,
                FullTimeHomeScore = m.FTHG,
                FullTimeAwayScore = m.FTAG,
                HalftimeHomeScore = m.HTHG,
                HalftimeAwayScore = m.HTAG
            });
            _historicalGames.AddRange(currentFileGames);
        }
        
        var filePath = Path.Combine(_options.Historical, "historical.csv");
        await CreateCsvFiles(_historicalGames, filePath, token);
    }

    public async Task CreateUpcomingFixtureBy(CancellationToken token)
    {
        // Read team name csv to find the right name
        var teamNameFile = Path.Combine(_options.FilesDir, "teamnames.csv");
        var knwonTeamNames = ReadCsvFileBy<TeamName>(teamNameFile).ToList();
        // Query upcoming matches based on day and league
        var fixtures = new List<Game>();
        var leagues = new[]
        {
            new { League = League.BL1, MatchDay = 21 },
            new { League = League.PL, MatchDay = 25 },
            new { League = League.PD, MatchDay = 23 },
            new { League = League.SA, MatchDay = 24 },
            new { League = League.ELC, MatchDay = 34 },
            new { League = League.FL1, MatchDay = 25 },
        };

        foreach (var league in leagues)
        {
            var matches = await _footballApi.GetUpcomingMatchesBy(league.League, league.MatchDay, token);

            if (matches == null)
                continue;

            matches = matches.Select(m => new Game
            {
                Home = knwonTeamNames.Where(ii => ii.NewName == m.Home).Select(ia => ia.Name).First(),
                Away = knwonTeamNames.Where(ii => ii.NewName == m.Away).Select(ia => ia.Name).First(),
                League = m.League.ToString(),
                GameDay = m.GameDay,
                DateTime = m.DateTime
            }).ToList();

            fixtures.AddRange(matches);
        }
        
        var filePath = Path.Combine(_options.Upcoming, "fixtures.csv");
        await CreateCsvFiles(fixtures,filePath, token);
        //TODO: File creating error permission denied
        //await CreateExcelFiles(fixtures, token);
    }

    public List<Game> GetHistoricalGamesBy(DateTime endDate)
    {
        var files = Directory.GetFiles(_options.Historical);
        var games = ReadCsvFileBy(endDate, files);

        return games;
    }
    
    public List<Game> GetUpcomingGamesBy(DateTime endDate)
    {
        var files = Directory.GetFiles(_options.Upcoming);
        var games = ReadCsvFileBy(endDate, files);

        return games;
    }

    private static List<Game> ReadCsvFileBy(DateTime endDate, IEnumerable<string> files)
    {
        var games = new List<Game>();
        foreach (var file in files)
        {
            var currentFileGames = ReadCsvFileBy<Game>(file)
                .GetHistoricalGamesOrderByDateBy(endDate);
            
            games.AddRange(currentFileGames);
        }
        return games;
    }
    
    private static IEnumerable<T> ReadCsvFileBy<T>(string file) where T : class
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower()
        };
        using var reader = new StreamReader(file);
        using var csv = new CsvReader(reader, config);
        var records =  csv.GetRecords<T>().ToList();

        return records;
    }

    private static async Task CreateCsvFiles(IEnumerable<Game> fixtures, string filePath, CancellationToken token)
    {
        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(fixtures, token);
    }

    
    private async Task CreateExcelFiles(IEnumerable<Game> fixtures,string filePath, CancellationToken token)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(filePath);
        // Create a new worksheet
        var worksheet = package.Workbook.Worksheets.Add("fixtures");

        // Load data into the worksheet
        worksheet.Cells.LoadFromCollection(fixtures, true);

        // Save the Excel workbook to a file
        await package.SaveAsync(token);
    }
}
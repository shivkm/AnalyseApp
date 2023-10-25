using System.Globalization;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Options;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;

namespace AnalyseApp.Services;

public class FileProcessor: IFileProcessor
{
    private readonly FileProcessorOptions _options;
    
    public FileProcessor(IOptions<FileProcessorOptions> options)
    {
        _options = options.Value;
    }
    
    public List<Matches> GetHistoricalMatchesBy()
    {
        var files = Directory.GetFiles(_options.RawCsvDir, "*.csv");
        var games = new List<Matches>();
        foreach (var file in files)
        {
            var currentFile= ReadCsvFileBy<Matches>(file);
            games.AddRange(currentFile);
        }

        return games;
    }

    public List<Matches> GetUpcomingGamesBy(string fixtureFileName)
    {
        var filePath = Path.Combine(_options.Upcoming, fixtureFileName);

        // Return an empty list if the file doesn't exist.
        if (!File.Exists(filePath)) return new List<Matches>();
        
        var games = ReadCsvFileBy<Matches>(filePath).ToList();
        return games;

    }

    public void CreateFixtureBy(string startDate, string endDate)
    {
        // Parse the start date using the expected format "dd/MM/yy"
        var startDateTime =startDate.Parse();

        // Parse the end date using the expected format "dd/MM/yy"
        var endDateTime = endDate.Parse();

        var historicalData = GetHistoricalMatchesBy();

        var selectedMatches = historicalData
            .Where(match => IsWithinDateRange(match, startDateTime, endDateTime))
            .OrderByDescending(match =>
            {
                var parsedDate = match.Date.Parse();
                return parsedDate;
                
            })
            .ToList();

        WriteSelectedMatchesToCsv(selectedMatches, $"fixture-{startDateTime.Date.Day}-{startDateTime.Date.Month}");
    }

    private static bool IsWithinDateRange(Matches match, DateTime startDate, DateTime endDate)
    {
        var matchStartDate = match.Date.Parse();
        return matchStartDate >= startDate && matchStartDate <= endDate;
    }

    private void WriteSelectedMatchesToCsv(IEnumerable<Matches> matches, string fileName)
    {
        var filePath = Path.Combine(_options.Upcoming, fileName);
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.WriteRecords(matches);
    }
    
    private static IEnumerable<T> ReadCsvFileBy<T>(string file)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower()
        };
        using var reader = new StreamReader(file);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<T>().ToList();

        return records;
    }
}

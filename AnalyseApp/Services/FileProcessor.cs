using System.Globalization;
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
        var files = Directory.GetFiles(_options.RawCsvDir);
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
        var startDateTime = DateTime.ParseExact(startDate, "dd/MM/yy", CultureInfo.InvariantCulture);

        // Parse the end date using the expected format "dd/MM/yy"
        var endDateTime = DateTime.ParseExact(endDate, "dd/MM/yy", CultureInfo.InvariantCulture);

        var historicalData = GetHistoricalMatchesBy();

        var selectedMatches = historicalData
            .Where(match => IsWithinDateRange(match, startDateTime, endDateTime))
            .OrderByDescending(match =>
            {
                DateTime parsedDate;
                if (DateTime.TryParseExact(match.Date, "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate) ||
                    DateTime.TryParseExact(match.Date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    return parsedDate;
                }
                return DateTime.MinValue;
            })
            .ToList();

        WriteSelectedMatchesToCsv(selectedMatches, $"fixture-{startDateTime.Date.Day}-{startDateTime.Date.Month}");
    }

    private static bool IsWithinDateRange(Matches match, DateTime startDate, DateTime endDate)
    {
        var matchStartDate = DateTime.ParseExact(match.Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
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

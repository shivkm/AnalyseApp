﻿using System.Globalization;
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
    
    public List<Game> MapMatchesToGames(IEnumerable<Matches> matches)
    {
        return matches
            .Select(match => new Game
            {
                League = match.Div,
                Date = match.Date,
                HomeTeam = match.HomeTeam,
                AwayTeam = match.AwayTeam,
                FullTimeGoal = match.FTHG + match.FTAG, 
                HalfTimeGoal = match.HTHG + match.HTAG,
            })
            .ToList();
    }

    public void CreateCsvFile(IEnumerable<Game> games)
    {
        using var writer = new StreamWriter(_options.MachineLearning);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.WriteRecords(games);
    }

    public List<Matches> GetUpcomingGames()
    {
        var files = Directory.GetFiles(_options.Upcoming);
        var games = new List<Matches>();
        foreach (var file in files)
        {
            var currentFile= ReadCsvFileBy<Matches>(file);
            games.AddRange(currentFile);
        }

        return games;
    }

    
    public List<Game> GetHistoricalGames()
    {
        var files = Directory.GetFiles(_options.MachineLearning);
        var games = new List<Game>();
        foreach (var file in files)
        {
            var currentFile= ReadCsvFileBy<Game>(file);
            games.AddRange(currentFile);
        }

        return games;
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

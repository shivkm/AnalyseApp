using AnalyseApp.Models;
using AnalyseApp.Services;
using FluentAssertions;

namespace AnalyseAppUnitTests;

public class HeadToHeadPredictionTests
{
    [Fact]
    public void HeadToHeadNaiveBayesBy()
    {
        // ARRANGE
        const string homeTeam = "FC Koln";
        const string awayTeam = "RB Leipzig";
        var mockData = GetMockDataBy(homeTeam, awayTeam).ToList();
        var service = new HeadToHeadService();
        
        //ACT
        service.HeadToHeadNaiveBayesBy(mockData, homeTeam, awayTeam);
        
        // 
    }
    private IEnumerable<HistoricalGame> GetMockDataBy2(string homeTeam, string awayTeam)
    {
        var mock = new List<HistoricalGame>
        {
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 0,
                FTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 2,
                FTAG = 1
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 1,
                FTAG = 2
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 1,
                FTAG = 4
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 0,
                FTAG = 1
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 4,
                FTAG = 0
            }
        };
        
        return mock;
    }

    private IEnumerable<HistoricalGame> GetMockDataBy(string homeTeam, string awayTeam)
    {
        var mock = new List<HistoricalGame>
        {
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 2,
                FTAG = 2
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 3,
                FTAG = 1
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 1,
                FTAG = 1
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 2,
                FTAG = 1
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 0,
                FTAG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 2,
                FTAG = 4
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 4,
                FTAG = 1
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 1,
                FTAG = 2
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 1,
                FTAG = 2
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 3,
                FTAG = 1
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 1,
                FTAG = 1
            }
        };
        
        return mock;
    }
}
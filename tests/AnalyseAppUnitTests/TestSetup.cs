using AnalyseApp.Models;

namespace AnalyseAppUnitTests;

public class TestSetup
{
    protected static IEnumerable<HistoricalGame> GetMockDataBy(string homeTeam, string awayTeam)
    {
        var mock = new List<HistoricalGame>
        {
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 2,
                FTAG = 2,
                HTHG = 1,
                HTAG = 1
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 3,
                FTAG = 1,
                HTAG = 1,
                HTHG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 1,
                FTAG = 1,
                HTAG = 0,
                HTHG = 0 
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 2,
                FTAG = 1,
                HTAG = 0,
                HTHG = 0
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 0,
                FTAG = 0,
                HTAG = 0,
                HTHG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 2,
                FTAG = 4,
                HTAG = 1,
                HTHG = 2
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 4,
                FTAG = 1,
                HTAG = 3,
                HTHG = 1
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 1,
                FTAG = 2,
                HTAG = 1,
                HTHG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 1,
                FTAG = 2,
                HTAG = 0,
                HTHG = 1
            },
            new HistoricalGame
            {
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                FTHG = 3,
                FTAG = 1,
                HTAG = 1,
                HTHG = 0
            },
            new HistoricalGame
            {
                HomeTeam = awayTeam,
                AwayTeam = homeTeam,
                FTHG = 1,
                FTAG = 1,
                HTAG = 1,
                HTHG = 1
            }
        };
        
        return mock;
    }
}
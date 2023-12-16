using AnalyseApp.Models;

namespace ScoreMind.Application.Extensions;

public static class DataProcessorExtensions
{
    internal static IEnumerable<Game> GetHistoricalGamesBy(this IEnumerable<Game> historicalGames, string teamName, bool isHome = false)
    {
        var teamHistoricalGames = historicalGames
            .Where(item => isHome ? item.HomeTeam == teamName : item.AwayTeam == teamName)
            .ToList();

        return teamHistoricalGames;

    }

}
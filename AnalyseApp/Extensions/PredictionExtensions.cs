using AnalyseApp.models;

namespace AnalyseApp.Extensions;

public static class PredictionExtensions
{
    internal static IEnumerable<Match> OrderMatchesBy(this List<Match> matches, DateTime playDate)
    {
        matches = matches.Where(i =>
            {
                var matchDate = i.Date.Parse();
                return matchDate < playDate;
            })
            .OrderByDescending(i => i.Date.Parse())
            .ToList();

        return matches;
    }
}
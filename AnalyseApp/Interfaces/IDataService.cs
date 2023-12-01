using AnalyseApp.models;
using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IDataService
{
    Match GetTeamMatchAverageBy(List<Matches> historicalMatches, string team);

    Head2HeadAverage HeadToHeadAverageBy(List<Matches> historicalMatches, string homeTeam, string awayTeam);
}
using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IDataService
{
    HeadToHeadData GetHeadToHeadDataBy(string homeTeam, string awayTeam, DateTime playedOn);

    TeamData GetTeamDataBy(string teamName, IList<Matches> data);
}
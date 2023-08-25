using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IDataService
{
    HeadToHeadData GetHeadToHeadDataBy(string homeTeam, string awayTeam, string playedOn);
    
    TeamData GetTeamDataBy(string teamName, IEnumerable<Matches> data);
}
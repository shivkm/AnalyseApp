using AnalyseApp.models;
using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IDataService
{
    HeadToHeadData GetHeadToHeadDataBy(string homeTeam, string awayTeam, DateTime playedOn);

    TeamData GetTeamDataBy(string teamName, IList<Matches> data);
    TeamGoalAverage CalculateTeamGoalAverageBy(string teamName, IList<Matches> data);
    HeadToHeadGoalAverage CalculateHeadToHeadAverageBy(string homeTeam, string awayTeam, DateTime playedOn);
}
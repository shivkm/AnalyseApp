using AnalyseApp.Enums;
using AnalyseApp.models;
using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IMatchPredictor
{
    List<Ticket> GenerateTicketBy(int gameCount, int ticketCount, BetType type, string fixture = "fixture-24-11");
    void GenerateFixtureFiles(string fixtureName);
    Prediction Execute(Matches matches, BetType? betType = BetType.Unknown);
    MatchGoalsData GetTeamSeasonGoals(string home, string away, DateTime playedOnDateTime);
}
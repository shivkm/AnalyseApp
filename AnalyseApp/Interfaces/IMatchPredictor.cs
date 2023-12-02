using AnalyseApp.Enums;
using AnalyseApp.models;
using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IMatchPredictor
{
    List<Ticket>? GenerateTicketBy(int gameCount, int ticketCount, BetType type, string fixture);
    void GenerateFixtureFiles(string fixtureName);
    Prediction Execute(Match match);
}
using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IAnalyseHandler
{
    IAnalyseHandler SetNext(IAnalyseHandler handler);
    GameQualification HandleRequest(List<HistoricalGame> pastGames, GameQualification gameQualification, string homeTeam, string awayTeam);
}
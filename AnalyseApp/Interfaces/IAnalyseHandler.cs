using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IAnalyseHandler
{
    IAnalyseHandler? SetNext(IAnalyseHandler? analyseHandler);
    bool? Handle(double probability, NextGame lastSixGames, NextGame allGames);
}
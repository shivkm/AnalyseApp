using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IPoissonService
{
    double GetProbabilityBy(string teamName, bool atHome, bool currentForm, List<Matches> historicalMatches);
}
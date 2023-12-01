using AnalyseApp.Interfaces;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class DataProcessor : IDataProcessor
{
    private readonly List<Matches> _historicalMatches;

    public DataProcessor(IFileProcessor fileProcessor)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
    }
    
    
}
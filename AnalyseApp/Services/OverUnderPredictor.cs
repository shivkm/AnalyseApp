using AnalyseApp.Interfaces;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class OverUnderPredictor: IOverUnderPredictor
{
    private readonly IFileProcessor _fileProcessor;
    private List<Game> _historicalGames = new();

    public OverUnderPredictor(IFileProcessor fileProcessor)
    {
        _fileProcessor = fileProcessor;
    }

    public void CreateFiles()
    {
        var historicalMatches = _fileProcessor.GetHistoricalMatchesBy();
        _historicalGames = _fileProcessor.MapMatchesToGames(historicalMatches);
        
        _fileProcessor.CreateCsvFile(_historicalGames);
    }
    
    
}
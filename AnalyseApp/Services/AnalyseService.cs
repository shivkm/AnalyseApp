using AnalyseApp.models;

namespace AnalyseApp.Services;

public class AnalyseService
{
    private List<GameData> _gameData;

    public AnalyseService(List<GameData> gameData)
    {
        _gameData = gameData;
    }
}
using AnalyseApp.Extensions;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class AnalyseService
{
    private readonly List<GameData> _gameData;
    private readonly List<Game> _bettingGames = new ();

    public AnalyseService(List<GameData> gameData)
    {
        _gameData = gameData;
    }

    public Game AnalyseGameBy(string homeTeam, string awayTeam)
    {
        var headToHead = _gameData.AnalyseHeadToHead(homeTeam, awayTeam);
        var lastTwelveGames = AnalyseGames(homeTeam, awayTeam, 12);
        var lastSixGames = AnalyseGames(homeTeam, awayTeam, 6);
        var allGames = AnalyseGames(homeTeam, awayTeam, 0);
        var result = new Game
        {
            Title = $"{homeTeam}:{awayTeam}",
            LastTwelveGames = lastTwelveGames,
            LastSixGames = lastSixGames,
            AllGame = allGames,
            HeadToHead = headToHead
        };

        var predictBothTeamScore = allGames
            .PredictBothTeamScore(lastTwelveGames, lastSixGames, headToHead);
        
        var predictMoreThanTwoScore = allGames
            .PredictMoreThanTwoScore(lastTwelveGames, lastSixGames, headToHead);
        
        var predictTwoToThreeScore = allGames
            .PredictTwoToThreeScore(lastTwelveGames, lastSixGames, headToHead);

        var predict = $"{result.Title}";
        if (predictBothTeamScore.Average > predictMoreThanTwoScore.Average && predictBothTeamScore.Qaulified)
        {
            _bettingGames.Add(result);
            predict = $" {predict} is both team make score because analysis says over " +
                      $"{predictBothTeamScore.Average}% score both team at least one goal each.";
        }
        if (predictMoreThanTwoScore is { Average: > 60, Qaulified: true })
        {
            _bettingGames.Add(result);
            predict = $"{predict}\n{result.Title} is both team make score because analysis says over" +
                      $" {predictMoreThanTwoScore.Average}% score both team at least one goal each.";
        }

        if (!string.IsNullOrWhiteSpace(predictTwoToThreeScore))
        { 
            _bettingGames.Add(result);
            predict = $"{predict}\n{predictTwoToThreeScore}";
        }

        result.Prediction = predict;
        return result;
    }
    
    private GameAverage AnalyseGames(string homeTeam, string awayTeam, int takeLastGames)
    {
        var nextHomeGame = new NextGame(takeLastGames, 60, 55)
        {
            Team = homeTeam,
            Msg = default!,
            IsHome = true
        };
        var nextAwayGame = new NextGame(takeLastGames, 60, 55)
        {
            Team = awayTeam,
            Msg = default!,
            IsHome = default
        };
        var result = new GameAverage
        {
            Home = _gameData.GetGamesDataBy(nextHomeGame).AnalyseGamesBy(nextHomeGame),
            Away = _gameData.GetGamesDataBy(nextAwayGame).AnalyseGamesBy(nextAwayGame)
        };

        return result;
    }
}
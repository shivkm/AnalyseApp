using System.Net.Http.Json;
using PredictionTool.Enums;
using PredictionTool.Interfaces;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class FootballApi: IFootballApi
{
    private readonly HttpClient _client;
    
    public FootballApi(HttpClient client)
    {
        _client = client;
    }
    
    /// <summary>
    /// Query the upcoming matches
    /// </summary>
    /// <param name="league"></param>
    /// <param name="matchDay"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<List<Game>?> GetUpcomingMatchesBy(
        League league, int matchDay, CancellationToken token)
    {
        var response = await _client.GetFromJsonAsync<MatchesResponse>(
            $"{league}/matches?matchday={matchDay}&season=2022", token);

        var games = CreateGamesBy(response.Matches, league);
        return games;
    }

    private static List<Game> CreateGamesBy(IEnumerable<Match> matches, League league)
    {
        var games = matches.Select(record => new Game
        {
            Home = record.HomeTeam.ShortName,
            Away = record.AwayTeam.ShortName,
            League = GetLeagueShortName(league),
            GameDay = record.Matchday,
            DateTime = record.UtcDate
        });

        return games.ToList();
    }
    
    private static string GetLeagueShortName(League league)
    {
        return league switch
        {
            League.PL => "E0",
            League.SA => "I1",
            League.ELC => "E1",
            League.PD => "SP1",
            League.BL1 => "D1",
            League.FL1 => "F1",
            _ => throw new ArgumentOutOfRangeException(nameof(league), league, null)
        };
    }
}
using System.Net.Http.Json;
using AnalyseApp.Interfaces;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class FootballService: IFootballService
{
    private readonly HttpClient _httpClient;

    public FootballService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    public async Task<ApiResponse> QueryAndSaveLeaguesBy()
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse>("leagues");
        return response;
    }
}
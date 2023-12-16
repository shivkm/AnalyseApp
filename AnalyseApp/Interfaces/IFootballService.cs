using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IFootballService
{
    Task<ApiResponse> QueryAndSaveLeaguesBy();
}
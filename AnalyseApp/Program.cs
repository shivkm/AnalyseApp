using AnalyseApp.Services;

var analysis = new AnalyseService();
analysis.ReadHistoricalGames().ReadUpcomingGames().AnalyseGames();

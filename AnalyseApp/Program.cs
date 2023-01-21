using AnalyseApp.Commons.Enums;
using AnalyseApp.Services;

var analysis = new AnalyseService();

analysis.ReadHistoricalGames().ReadUpcomingGames()
    //.AnalyseMatches();
    .Analyse("Vallecano", "Sociedad", "SP1");
// .StartAnalysisTestBy("Osasuna", "Mallorca", "SP1")
   // .Analyse("Marseille", "Lorient", "F1");
 //.StartAnalysisTestBy("Sociedad", "Ath Bilbao", "SP1");

using AnalyseApp.Services;

var analysis = new AnalyseService();

analysis.ReadHistoricalGames().ReadUpcomingGames().AnalyseMatches();
// .StartAnalysisTestBy("Roma", Seriea.Fiorentina.ToString(), "I1")
// .StartAnalysisTestBy("Osasuna", "Mallorca", "SP1")
   // .Analyse("Marseille", "Lorient", "F1");
 //.StartAnalysisTestBy("Sociedad", "Ath Bilbao", "SP1");

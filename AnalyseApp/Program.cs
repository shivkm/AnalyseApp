using AnalyseApp.Commons.Enums;
using AnalyseApp.Services;

var analysis = new AnalyseService();

analysis.ReadHistoricalGames().ReadUpcomingGames().AnalyseGames();
 // .CreateMlFile("D1").CreateMlFile("E0")
 // .CreateMlFile("E1").CreateMlFile("E2")
 // .CreateMlFile("SP1").CreateMlFile("I1")
 // .CreateMlFile("F1").CreateMlFile("D2")
 //.Analyse("Freiburg", "Ein Frankfurt", "D1");
    //.AnalyseMatches();
    //.Analyse("Schalke 04", "RB Leipzig", "D1");
   // .Analyse("Hertha", "Wolfsburg", "D1");
    //.Analyse("Hoffenheim", "FC Koln", "D1");
   // .Analyse("Fulham", "Tottenham", "E0");
   // .Analyse("Valencia", "Almeria", "SP1");
    //.Analyse("Inter", "Empoli", "I1");
// .StartAnalysisTestBy("Osasuna", "Mallorca", "SP1")
   // .Analyse("Marseille", "Lorient", "F1");
 //.StartAnalysisTestBy("Sociedad", "Ath Bilbao", "SP1");

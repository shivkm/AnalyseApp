using AnalyseApp;
using AnalyseApp.Commons.Enums;
using AnalyseApp.Extensions;

var analysis = new Analyse();

analysis.ReadFilesHistoricalGames().ReadUpcomingGames().StartAnalysis();
//analysis.ReadFilesHistoricalGames().ReadUpcomingGames()
  //  .StartAnalysisBy("Charlton", "Barnsley")
   // .StartAnalysisBy(Seriea.Bologna.GetValue(), Seriea.Atalanta.GetValue())
   // .StartAnalysisBy(Laliga.Sevilla.GetValue(), Laliga.Getafe.GetValue())
    ;

using AnalyseApp;
using Microsoft.ML.Data;

var analysis = new Analyse();

//analysis.ReadFilesHistoricalGames().ReadUpcomingGames().StartAnalysis();
analysis.ReadFilesHistoricalGames().ReadUpcomingGames().AnalyseBy("Troyes", "Marseille");

//await analysis.CreateCsvFile();
//analysis.Test();

public class Prediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
}

//TODO: 
// Method for single match analysis
// 
using AnalyseApp;
using Microsoft.ML.Data;

var analysis = new Analyse();

analysis.ReadFilesHistoricalGames().ReadUpcomingGames().StartAnalysis();

//await analysis.CreateCsvFile();
//analysis.Test();

public class Prediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
}
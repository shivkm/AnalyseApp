namespace AnalyseApp.Models;


public record Prediction(
    string Key,
    string League,
    double? RajevWeighting, 
    double WkSmWeighting, 
    double? ShivWeighting,
    string? Msg = "");

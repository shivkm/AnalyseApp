using AnalyseApp.Enums;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Interfaces;

/// <summary>
/// a comprehensive handling of machine learning processes.
/// </summary>
public interface IMachineLearningEngine
{
    /// <summary>
    /// Prepare the historical data for ml
    /// </summary>
    /// <param name="historicalMatches"></param>
    /// <returns></returns>
    void PrepareDataBy(IEnumerable<MatchAverage> teamsAverageData);
    
    /// <summary>
    /// Create set of train and test data
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    ITransformer TrainModel(PredictionType type);
    
    void SaveModel(string modelPath);
    void LoadModel(string modelPath);
    
    MatchOutcomePrediction PredictOutcome(MatchAverage matchData, PredictionType type);
}
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
    void PrepareDataBy(IEnumerable<Match> teamsAverageData);
    
    /// <summary>
    /// Create set of train and test data
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    ITransformer TrainModel(PredictionType type);
    
    void SaveModel(ITransformer model, string modelPath);
    ITransformer LoadModel(string modelPath);
    double EvaluateModel(ITransformer model, PredictionType type);
    PredictionType PredictOutcome(Match match, ITransformer model, PredictionType type);
}
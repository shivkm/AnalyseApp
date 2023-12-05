using AnalyseApp.models;
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
    IDataView PrepareDataBy(IEnumerable<Match> historicalMatches);
    
    /// <summary>
    /// Create set of train and test data
    /// </summary>
    /// <param name="dataView"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    ITransformer TrainModel(IDataView dataView, string type);
    
    /// <summary>
    /// Split the DataView into training and testing set
    /// </summary>
    /// <param name="dataView"></param>
    /// <returns></returns>
    (IDataView trainSet, IDataView testSet) SplitData(IDataView dataView);
    
    
    void SaveModel(ITransformer model, IDataView trainingDataView, string modelPath);
    ITransformer LoadModel(string modelPath);
    double EvaluateModel(ITransformer model, IDataView testData, string type);
    bool PredictOutcome(Match newMatch, ITransformer model, string type);
}
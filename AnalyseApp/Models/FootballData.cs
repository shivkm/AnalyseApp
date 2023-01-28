using Microsoft.ML.Data;

namespace AnalyseApp.Models;


// Define a class to hold the data for a single match
public class FootballData
{
    [LoadColumn(0)] public float HomeTeamScore;
    [LoadColumn(1)] public float AwayTeamScore;
    [LoadColumn(2)] public float Label;
}

// Define a class to hold the input and output data for the model

using Microsoft.ML.Data;

namespace AnalyseApp.models;

public record GameData
{
    public string Date { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public int? FTHG { get; set; }
    public int? FTAG { get; set; }
    public string FTR { get; set; }
    public int? HTHG { get; set; }
    public int? HTAG { get; set; }
    public string HTR { get; set; }
}

// Define a class to hold the data for each match
public class FootballGameData
{
    [LoadColumn(0)] public float HomeTeamGoals;
    [LoadColumn(1)] public float AwayTeamGoals;
    [LoadColumn(2)] public float HomeTeamHalfTimeGoals;
    [LoadColumn(3)] public float AwayTeamHalfTimeGoals;
    [LoadColumn(4)] public bool HomeTeamWins;
    [LoadColumn(5)] public float[] Score;
}

public class FootballGamePrediction
{
    [ColumnName("Score")]
    public float[] Score;
}

/*
// Define a class to hold the data from the input file
public class FootballGameData
{
    [LoadColumn(0)] public float Team1Score;
    [LoadColumn(1)] public float Team2Score;
    [LoadColumn(2)] public float Team1Shots;
    [LoadColumn(3)] public float Team2Shots;
    [LoadColumn(4)] public bool Result;
}

// Define a class to hold the data for the model's output
public class FootballGamePrediction
{
    [ColumnName("PredictedLabel")]
    public bool Result;
}


var pipeline = context.Transforms.Conversion.MapValueToKey("Label", "Result")
    .Append(context.Transforms.Concatenate("Features", "Team1Score", "Team2Score", "Team1Shots", "Team2Shots"))
    .Append(context.Transforms.NormalizeMinMax("Features"))
    .Append(context.Transforms.Conversion.MapKeyToValue("Label"))
    .Append(context.BinaryClassification.Trainers.FastTree());

// Train the model
var model = pipeline.Fit(gameDataView);



// Evaluate the model's performance
Console.WriteLine($"Accuracy:");

*/
using CsvHelper.Configuration.Attributes;
using Microsoft.ML.Data;

namespace AnalyseApp.Models;

public record GameData
{
    public string Div { get; set; } = default!;
    public string Date { get; set; } = default!;
    [Optional]
    public string Time { get; set; } = default!;
    public string HomeTeam { get; set; } = default!;
    public string AwayTeam { get; set; } = default!;
    public int? FTHG { get; set; } = default!;
    public int? FTAG { get; set; } = default!;
    public string FTR { get; set; } = default!;
    public int? HTHG { get; set; } = default!;
    public int? HTAG { get; set; } = default!;
    public string HTR { get; set; } = default!;
    [Optional]
    public int? HS { get; set; }
    [Optional]
    public int? AS { get; set; }
    [Optional]
    public int? HST { get; set; }
    [Optional]
    public int? AST { get; set; }
    [Optional]
    public int? HO { get; set; }
    [Optional]
    public int? AO { get; set; }
    [Optional]
    public int? HF { get; set; }
    [Optional]
    public int? AF { get; set; }
    [Optional]
    public string HomeWin { get; set; } = default!;
    [Optional]
    public string Draw { get; set; } = default!;
    [Optional]
    public string AwayWin { get; set; } = default!;
    [Optional]
    public string MoreThanTwoGoals { get; set; } = "0";
    [Optional]
    public string LessThanTwoGoals { get; set; } = "0"!;
    [Optional]
    public string BothTeamScore { get; set; } = "0"!;
    [Optional]
    public string TwoToThree { get; set; } = "0"!;
}

// Define a class to hold the data for each match

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
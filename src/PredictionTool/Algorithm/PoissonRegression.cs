using Microsoft.ML;
using Microsoft.ML.Data;
using PredictionTool.Models;

namespace PredictionTool.Algorithm;

public class PoissonRegression
{
    private PredictionEngine<Features, Prediction> _model;

    public PoissonRegression(List<Game> games)
    {
        var context = new MLContext();

        var pipeline = context.Transforms.Concatenate("Features", "HomeTeamAttackStrength", "AwayTeamDefenseStrength")
            .Append(context.Transforms.Conversion.ConvertType("Label", outputKind: DataKind.Single))
            .Append(context.Regression.Trainers.LbfgsPoissonRegression());

        var dataView = context.Data.LoadFromEnumerable(games);

        var model = pipeline.Fit(dataView);

        _model = context.Model.CreatePredictionEngine<Features, Prediction>(model);
    }

    public double Predict(string homeTeam, string awayTeam, double homeTeamAttackStrength, double awayTeamDefenseStrength)
    {
        var input = new Features
        {
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            HomeTeamAttackStrength = homeTeamAttackStrength,
            AwayTeamDefenseStrength = awayTeamDefenseStrength
        };

        var prediction = _model.Predict(input);

        return prediction.Score;
    }

    private class Features
    {
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public double HomeTeamAttackStrength { get; set; }
        public double AwayTeamDefenseStrength { get; set; }
    }

    private class Prediction
    {
        public float Score { get; set; }
    }
}
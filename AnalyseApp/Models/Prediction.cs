using AnalyseApp.Enums;

namespace AnalyseApp.models;

public record Prediction(string Msg, bool Qualified, double Percentage, BetType Type);

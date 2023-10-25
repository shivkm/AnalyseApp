using AnalyseApp.Enums;
using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IMatchPredictor
{
    void Execute();
    double GetPredictionAccuracyRate(string fixtureName);
    Prediction Execute(Matches matches, BetType? betType = BetType.Unknown);
    MatchGoalsData GetTeamSeasonGoals(string home, string away, DateTime playedOnDateTime);
}
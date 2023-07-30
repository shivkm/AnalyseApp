using AnalyseApp.models;

namespace AnalyseApp.Interfaces;

public interface IAnalyseService
{
     MatchStatistic PrepareMatchStatisticsBy(string homeTeam, string awayTeam);
     Probability AnalyseOverTwoGoalProbabilityBy(MatchStatistic homeMatch);
     void CalculationAnalysis();

     (double scoreProbability, double noScoreProbability) Test2(
         double homeGoalScored, double homeGoalConceded, double awayGoalScored, double awayGoalConceded);
}
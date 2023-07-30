using System.ComponentModel;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
namespace AnalyseApp.Services;

public class AnalyseService : IAnalyseService
{
    private readonly List<Matches> _historicalMatches;

    private readonly IFileProcessor _fileProcessor;
    public AnalyseService(IFileProcessor fileProcessor)
    {
        _fileProcessor = fileProcessor;
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
    }

    public void CalculationAnalysis()
    {
        var matchFactors = _fileProcessor.GetMatchFactors();
        var rightGame = new List<string>();
        var wrongGames = new List<string>();

        foreach (var matchFactor in matchFactors)
        {
            if (matchFactor.League != "Premier League") continue;
            
            var sessionAnalysis = ModifiedPoissonAnalysisBy(
                matchFactor.HomeGoalScored.ToDouble(), matchFactor.HomeGoalConceded.ToDouble(),
                matchFactor.AwayGoalScored.ToDouble(), matchFactor.AwayGoalConceded.ToDouble()
            );
        
            var lastSixHomeAnalysis = ModifiedPoissonAnalysisBy(
                matchFactor.HomeHomeGoalScored.ToDouble(), matchFactor.HomeHomeGoalConceded.ToDouble(),
                matchFactor.HomeAwayGoalScored.ToDouble(), matchFactor.HomeAwayGoalConceded.ToDouble()
            );
        
            var lastSixAwayAnalysis = ModifiedPoissonAnalysisBy(
                matchFactor.AwayHomeGoalScored.ToDouble(), matchFactor.AwayHomeGoalConceded.ToDouble(),
                matchFactor.AwayAwayGoalScored.ToDouble(), matchFactor.AwayAwayGoalConceded.ToDouble()
            );
        
            var headToHeadAnalysis = ModifiedPoissonAnalysisBy(
                (matchFactor.HeadToHeadAwayGoalScored ?? 0).ToDouble(), (matchFactor.HeadToHeadHomeGoalConceded ?? 0).ToDouble(),
                (matchFactor.HeadToHeadAwayGoalScored ?? 0).ToDouble(), (matchFactor.HeadToHeadAwayGoalConceded ?? 0).ToDouble()
            );

            var scoreProbability = sessionAnalysis.scoreProbability * 0.20 + lastSixHomeAnalysis.scoreProbability * 0.30 +
                                    lastSixAwayAnalysis.scoreProbability * 0.30 + headToHeadAnalysis.scoreProbability * 0.20;
            
            var noScoreProbability = sessionAnalysis.noScoreProbability * 0.20 + lastSixHomeAnalysis.noScoreProbability * 0.30 +
                                      lastSixAwayAnalysis.noScoreProbability * 0.30 + headToHeadAnalysis.noScoreProbability * 0.20;
            
            if (scoreProbability >= 0.74 && noScoreProbability < 0.26)
            {
                if (matchFactor.HomeGoal + matchFactor.AwayGoal > 2)
                {
                    rightGame.Add($"{matchFactor.Name}: More than two goals correct");
                }
                else
                {
                    wrongGames.Add($"{matchFactor.Name}: More than two goals wrong");
                }
            }

            if (sessionAnalysis.scoreProbability > 0.60 && headToHeadAnalysis.scoreProbability > 0.60 &&
                lastSixHomeAnalysis.scoreProbability < 0.50 && lastSixAwayAnalysis.scoreProbability > 0.60 &&
                noScoreProbability is > 0.25 and < 0.40)
            {
                if (matchFactor is { AwayGoal: > 0, HomeGoal: < 0 })
                {
                    rightGame.Add($"{matchFactor.Name}: Away team score correct");
                }
                else
                {
                    wrongGames.Add($"{matchFactor.Name}: Away team score wrong");
                }
            }
        
        
            if (sessionAnalysis.scoreProbability > 0.60 && headToHeadAnalysis.scoreProbability > 0.60 &&
                lastSixHomeAnalysis.scoreProbability > 0.60 && lastSixAwayAnalysis.scoreProbability < 0.50 &&
                noScoreProbability is > 0.25 and < 0.40)
            {   
                if (matchFactor is { AwayGoal: < 0, HomeGoal: > 0 })
                {
                    rightGame.Add($"{matchFactor.Name}: home team score correct");
                }
                else
                {
                    wrongGames.Add($"{matchFactor.Name}: home team score wrong");
                }
            }
        }
        
        rightGame.ForEach(i => Console.WriteLine($"{i}\t"));
        wrongGames.ForEach(i => Console.WriteLine($"{i}\t"));
    }
    
    private static (double scoreProbability, double noScoreProbability) ModifiedPoissonAnalysisBy(
        double homeGoalScored, double homeGoalConceded,double awayGoalScored, double awayGoalConceded)
    {
        var homeLambda = (homeGoalScored + homeGoalConceded) / (homeGoalScored is 0 ? 2 : homeGoalScored);
        var awayLambda = (awayGoalScored + awayGoalConceded) / (awayGoalScored is 0 ? 2 : awayGoalScored);
        var scoreProb = PoissonAnalysisBy(homeLambda, awayLambda);
        var noScoreProb = PoissonAnalysisBy(homeLambda, awayLambda, false);

        var scoreProbability = scoreProb.Sum();
        var noScoreProbability = noScoreProb.Sum();

        return (scoreProbability, noScoreProbability);
    }

    private static IEnumerable<double> PoissonAnalysisBy(double homeLambda, double awayLambda, bool score = true)
    {
        const int maxGoals = 10;
        var probability = new List<double>();
        for (var homeGoals = 0; homeGoals <= maxGoals; homeGoals++)
        {
            for (var awayGoals = 0; awayGoals <= maxGoals; awayGoals++)
            {
                var probHomeGoals = PoissonProbability(homeLambda, homeGoals);
                var probAwayGoals = PoissonProbability(awayLambda, awayGoals);

                switch (score)
                {
                    case true when awayGoals > 0 && homeGoals > 0:
                    case false when awayGoals is 0 && homeGoals is 0 || awayGoals is 0 || homeGoals is 0:
                        probability.Add(probHomeGoals * probAwayGoals);
                        break;
                }
            }
        }

        return probability;
    }

    
    
    public (double scoreProbability, double noScoreProbability) Test2(
        double homeGoalScored, double homeGoalConceded,double awayGoalScored, double awayGoalConceded)
    {
        var test = 
            ModifiedPoissonAnalysisBy(homeGoalScored,homeGoalConceded, awayGoalScored, awayGoalConceded);

        return test;
    }


    public MatchStatistic PrepareMatchStatisticsBy(string homeTeam, string awayTeam)
    {
        var latestMatchDate = new DateTime(2023, 05, 18);
        var homeTeamMatches = _historicalMatches.GetLastTenGamesBy(homeTeam, latestMatchDate);
        var awayTeamMatches = _historicalMatches.GetLastTenGamesBy(awayTeam, latestMatchDate);
        var headToHeadMatches = _historicalMatches.GetHeadToHeadGamesBy(homeTeam, awayTeam);

        var homeStatistic = homeTeamMatches.GetTeamStatistics(homeTeam);
        var awayStatistic = awayTeamMatches.GetTeamStatistics(awayTeam);
        var headToHeadStatistic = headToHeadMatches.GetHeadToHeadStatistics(homeTeam, awayTeam);
        return new MatchStatistic(homeStatistic, awayStatistic, headToHeadStatistic, false);
    }
    

    public Probability AnalyseOverTwoGoalProbabilityBy(MatchStatistic matchStatistic)
    {
        double overProbability = CalculateOverProbability(
            (double)matchStatistic.HomeMatch.Scored.HomeSideAvg,
            (double)matchStatistic.AwayMatch.Scored.AwaySideAvg,
            (double)matchStatistic.HomeMatch.ConcededScored.HomeSideAvg,
            (double)matchStatistic.AwayMatch.ConcededScored.AwaySideAvg,
            2.5);

        Console.WriteLine($"Probability of over {2.5} goals: {overProbability:P}");
        var probability = new Probability(100, false);

        if (matchStatistic.HomeMatch.Scored.HomeSideAvg >= 0.68 &&
            matchStatistic.AwayMatch.Scored.AwaySideAvg >= 0.68)
        {
            probability = probability with { Qualified = true };
        }


        return probability;
    }

    public static double CalculateOverProbability(double homeScoreAvg, double awayScoreAvg, double homeConcededAvg,
        double awayConcededAvg, double threshold)
    {
        double homeExpectedGoals = homeScoreAvg * awayConcededAvg;
        double awayExpectedGoals = awayScoreAvg * homeConcededAvg;
        double totalExpectedGoals = homeExpectedGoals + awayExpectedGoals;

        double underProbability = CalculateUnderProbability(totalExpectedGoals, threshold);
        double overProbability = 1 - underProbability;

        return overProbability;
    }

    public static double CalculateUnderProbability(double totalExpectedGoals, double threshold)
    {
        // You can choose a statistical distribution and calculate the probability using its CDF.
        // Here's an example using the Normal distribution.
        double mean = totalExpectedGoals;
        double standardDeviation = Math.Sqrt(totalExpectedGoals);

        double underProbability = NormalDistribution.CumulativeDistribution(threshold, mean, standardDeviation);

        return underProbability;
    }
    
    private static double Factorial(int n)
    {
        // Calculate the factorial of n
        if (n <= 1)
            return 1;

        return n * Factorial(n - 1);
    }
    
    private static double PoissonProbability(double lambda, int k)
    {
        // Calculate the Poisson probability for k goals given a lambda value
        return Math.Pow(lambda, k) * Math.Exp(-lambda) / Factorial(k);
    }
}


/*
 * Predicting the score of a football match can be a challenging task, as there are many factors that can impact the outcome of the game. Here are a few tips that might be helpful:

Analyze the teams: Look at the strengths and weaknesses of each team, and consider how they might match up against each other. Take into account factors like the team's current form, their past performance against each other, and any injuries or other absences.

Look at the context of the match: Consider the importance of the match and any external factors that might affect the teams' performance. For example, a team might be more motivated to win if they are fighting for a spot in a tournament or trying to avoid relegation.

Consider the conditions: Think about how the weather and the state of the pitch might affect the teams' strategies and performance.

Make use of statistical models: There are a number of statistical models that can be used to predict the outcome of football matches. These models can take into account a variety of factors, including the teams' past performance and the importance of the match.

Get expert opinions: Look for analysis and predictions from experts in the field, such as journalists, former players, and coaches.

I hope that helps! Predicting the score of a football match is always going to involve some level of uncertainty, but by considering a range of factors, you can increase your chances of making an informed prediction.

There are several ways you can use machine learning (ML) to predict the outcome of a football match. Here are a few approaches you might consider:

Collect data on past football matches, including information about the teams, players, match conditions, and match outcomes. This data can be used to train a machine learning model to predict the outcome of future matches.

Use statistical modeling techniques to analyze the data and identify patterns that may be predictive of match outcomes. This could include analyzing the strength of each team's defense, the scoring abilities of their players, or the impact of home field advantage.

Train a machine learning model on the data, using algorithms such as decision trees, random forests, or neural networks. These models can learn from the data and make predictions about the likelihood of different outcomes for future matches.

Evaluate the performance of the machine learning model using metrics such as accuracy, precision, and recall. You may need to fine-tune the model by adjusting its hyper parameters or by collecting more data in order to improve its performance.

Use the model to make predictions about the outcomes of future matches. You can also use the model to identify factors that are most important in determining the outcome of a match, which can help you understand why the model is making certain predictions.

There are many different types of data you can collect about football teams and their players that could be useful for a machine learning model to predict the outcome of a match. Some examples of data you might consider collecting include:

Team statistics: This could include data on the team's overall record (wins, losses, draws), goals scored and allowed, and other metrics that reflect the team's performance.

Player statistics: Data on the performance of individual players can be useful, such as goals scored, assists, tackles, and other metrics that reflect the player's contributions to the team.

Team and player ratings: You could also consider collecting data on the ratings of teams and players from various sources, such as sports websites or expert analysts. These ratings could reflect the overall strength of a team or the quality of individual players.

Match conditions: Data on the conditions of the match, such as the location (home field or away), the weather, and the surface of the field, could also be useful for predicting the outcome of a match.

Injuries: Information on the availability of key players, particularly due to injuries, could also be useful for predicting the outcome of a match.

It's important to note that the specific data you collect will depend on the goals of your model and the questions you are trying to answer. You may need to experiment with different types of data and features to find the combination that works best for your model.

 */

public static class NormalDistribution
{
    public static double CumulativeDistribution(double x, double mean, double standardDeviation)
    {
        double z = (x - mean) / standardDeviation;
        double cumulativeProbability = 0.5 * (1 + MathErf(z / Math.Sqrt(2)));

        return cumulativeProbability;
    }

    // Error function approximation using Abramowitz and Stegun formula
    private static double MathErf(double x)
    {
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        double sign = Math.Sign(x);
        x = Math.Abs(x);

        double t = 1.0 / (1.0 + p * x);
        double y = ((((a5 * t + a4) * t + a3) * t + a2) * t + a1) * t;

        return sign * (1 - y * Math.Exp(-x * x));
    }
}


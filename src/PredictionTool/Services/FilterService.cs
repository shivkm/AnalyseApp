using MathNet.Numerics.Distributions;
using PredictionTool.Extensions;
using PredictionTool.Interfaces;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class FilterService: IFilterService
{
    private const string LessThanTwoGoals = "LessThanTwoGoals";
    private const string TwoToThree = "TwoToThree";
    private const string BothTeamScore = "BothTeamScore";
    private const string MoreThanTwoGoals = "MoreThanTwoGoals";
    private const string Score = "Score";
    private const string ZeroGoal = "ZeroGoal";
    private const double MinScored = 0.60;
    private const double MaxConceded = 1.20;
    
    public FilterService() { }
    
    public (string Key, double Probability) FilterGames(
        QualifiedGames qualifiedGames, List<GameProbability> gameProbabilities, List<Game> historicalGames)
    {
        // Filter current season games
        var games = historicalGames.GetCurrentSeasonGamesBy(2022, 2023, qualifiedGames.League);
        
        // current form of both teams
        var currentForms = CalculateTeamsCurrentFormBy(qualifiedGames.Home, qualifiedGames.Away, historicalGames);

        // head to head accuracy
        var headToHeads = CalculateHeadToHeadBy(qualifiedGames.Home, qualifiedGames.Away, historicalGames);

        // calculate the team score mode
        var teamsMode = CalculateModeBy(qualifiedGames.Home, qualifiedGames.Away, games);

        // markov chain average used to calculate poisson prabability
        var homeMarkovChain = TeamMarkovChainProbability(historicalGames, qualifiedGames.Home);
        var awayMarkovChain = TeamMarkovChainProbability(historicalGames, qualifiedGames.Away);

        if (qualifiedGames.Home == "Inter")
        {
            
        }
        var overTwoGoals = OverTwoGoalsAnalysisBy(currentForms, headToHeads, teamsMode);
        if (overTwoGoals.Qualified)
        {
            Console.WriteLine($"{qualifiedGames.DateTime} {qualifiedGames.Home}:{qualifiedGames.Away} {overTwoGoals.Key}");
        }
        
        
        
        return ("", 0);
    }
    
    

    private static bool BothScoreAnalysisBy(TeamAccuracy currentForm, HeadToHead headToHead)
    {
        // Home and away both have 68% probability to score in current form
        if (currentForm is { HomeScoreProbability: > MinScored + 0.1, AwayScoreProbability: > MinScored + 0.1 })
            return true;
        
        // Head to head must strong
        return headToHead is { PlayedMatches: >= 4, BothScoredAvg: >= MinScored };
    }
    
    private static (bool Qualified, string Key) OverTwoGoalsAnalysisBy(TeamAccuracy currentForm, HeadToHead headToHead, (int home, int away) teamMode)
    {
        if (currentForm is { HomeScoreProbability: > MinScored + 0.1, AwayScoreProbability: > MinScored + 0.1 } and
            { AwayLastFiveOver: false, HomeLastFiveOver: false })
        {
            if (headToHead is { PlayedMatches: >= 4, MoreThanTwoGoalsAvg: >= MinScored })
            {
                return (true, "Over 2.5 Goals");
            }

            if (currentForm is not { HomeScoreProbability: > MinScored + 0.2, AwayScoreProbability: > MinScored + 0.2 } && 
                    teamMode is { home: > 0, away: 0 } or { home: 0, away: > 0 })
            {
                return (true, "Two to three goals");
            }
            if (currentForm is { HomeScoreProbability: > MinScored + 0.2, AwayScoreProbability: > MinScored + 0.2 } and
                { AwayLastFiveOver: false, HomeLastFiveOver: false })
            {
                return (true, "Over 2.5 Goals");
            }
        }
        if (currentForm is { HomeScoreProbability: > MinScored + 0.1, AwayScoreProbability: > MinScored + 0.1 } and 
            { AwayLastFiveOver: false, HomeLastFiveOver: false })
        {
            if (teamMode is { home: > 0, away: > 0 })
            {
                return (true, "Both team score");
            }
            
            if (headToHead is { PlayedMatches: >= 4, BothScoredAvg: >= MinScored })
            {
                return (true, "Both team score");
            }
        }
        if (currentForm is { AwayScoredAvg: < MinScored, AwayConcededAvg: > MaxConceded })
        {
            if (currentForm.HomeScoreProbability > currentForm.AwayScoreProbability &&
                currentForm is { HomeScoreProbability: >= MinScored + 0.1, AwayScoreProbability: < MinScored, HomeLastFiveWon: false })
            {
                return (true, "Home win the match");
            }
        }
        if (currentForm is { HomeScoredAvg: < MinScored, HomeConcededAvg: > MaxConceded })
        {
            if (currentForm.AwayScoreProbability > currentForm.HomeScoreProbability &&
                currentForm is { AwayScoreProbability: >= MinScored + 0.1, HomeScoreProbability: < MinScored, AwayLastFiveOver: false  })
            {
                return (true, "Away win the match");
            }
        }
        
        if (currentForm is { HomeScoredAvg: <= MinScored, HomeConcededAvg: < MaxConceded } or 
                            {AwayScoredAvg: <= MinScored, AwayConcededAvg: < MaxConceded})
        {
            if (currentForm is { AwayScoreProbability: <= MinScored, HomeScoreProbability: <= MinScored } &&
                headToHead is { PlayedMatches: > 2, MoreThanTwoGoalsAvg: < MinScored - 0.1 })
            {
                return (true, "less than three goals");
            }
        }

        return (false, "");
    }
    
    private static TeamAccuracy CalculateTeamsCurrentFormBy(string home, string away, List<Game> games)
    {
        // Last Six home and away games
        var homeCurrentGames = games.GetLastSixGamesBy(home);
        var awayCurrentGames = games.GetLastSixGamesBy(away);
        
        // Average of home score and average score
        var homeScoredAvg = homeCurrentGames.CalculateScoredGoalAccuracy(home);
        var homeConcededAvg = homeCurrentGames.CalculateConcededGoalAccuracy(home);
        var awayScoredAvg = awayCurrentGames.CalculateScoredGoalAccuracy(away);
        var awayConcededAvg = awayCurrentGames.CalculateConcededGoalAccuracy(away);
        
        var homeFinalAvg = homeScoredAvg * awayConcededAvg;
        var awayFinalAvg = awayScoredAvg * homeConcededAvg;

        var homeProbability = CalculateScoreProbabilityBy(homeFinalAvg);
        var awayProbability = CalculateScoreProbabilityBy(awayFinalAvg);

        // Average of scoring at least one goal
        var homeCurrentOneGoalAvg = homeCurrentGames
            .Count(i => i.Home == home && i.FullTimeHomeScore > 0 ||
                        i.Away == home && i.FullTimeAwayScore > 0)
            .Divide(homeCurrentGames.Count);
        
        var awayCurrentOneGoalAvg = awayCurrentGames
            .Count(i => i.Home == away && i.FullTimeHomeScore > 0 ||
                        i.Away == away && i.FullTimeAwayScore > 0)
            .Divide(awayCurrentGames.Count);

        var homeLastFiveGamesOver = homeCurrentGames.OrderByDescending(i => i.DateTime)
            .Take(5).All(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2);
        
        var awayLastFiveGamesOver = awayCurrentGames.OrderByDescending(i => i.DateTime)
            .Take(5).All(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2);

        var homeLastFiveGamesWon = homeCurrentGames.OrderByDescending(i => i.DateTime)
            .Take(5).All(i => i.FullTimeResult == "H" && i.Home == home || i.FullTimeResult == "A" && i.Away == home);
        
        var awayLastFiveGamesWon = awayCurrentGames.OrderByDescending(i => i.DateTime)
            .Take(5).All(i => i.FullTimeResult == "H" && i.Home == away || i.FullTimeResult == "A" && i.Away == away);
        
        return new TeamAccuracy(
            homeScoredAvg,
            awayScoredAvg, 
            homeConcededAvg,
            awayConcededAvg, 
            homeProbability,
            awayProbability,
            homeLastFiveGamesOver,
            awayLastFiveGamesOver,
            homeLastFiveGamesWon,
            awayLastFiveGamesWon
            );
    }

    private static double CalculateScoreProbabilityBy(double average)
    {
        var scoresProb = new List<double>();
        for (var i = 1; i <= 10; i++)
        {
            var prob = CalculatePoissonProbability(average, i);
            scoresProb.Add(prob);
        }

        return scoresProb.Sum();
    }
    
    private static HeadToHead CalculateHeadToHeadBy(string home, string away, List<Game> historicalGames)
    {
        var headToHeadGames = historicalGames
            .Where(i => i.Home == home && i.Away == away || i.Away == home && i.Home == away)
            .OrderByDescending(o => o.DateTime)
            .ToList();

        var bothScoredAvg = headToHeadGames
            .Count(i => i is { FullTimeHomeScore: > 0, FullTimeAwayScore: > 0 })
            .Divide(headToHeadGames.Count); 
        
        var moreThanTwoGoals = headToHeadGames
            .Count(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2)
            .Divide(headToHeadGames.Count); 
        
        var twoToThreeGoals = headToHeadGames
            .Count(i => i.FullTimeHomeScore + i.FullTimeAwayScore == 2 ||
                            i.FullTimeHomeScore + i.FullTimeAwayScore == 3)
            .Divide(headToHeadGames.Count);

        return new HeadToHead(headToHeadGames.Count, bothScoredAvg, moreThanTwoGoals, twoToThreeGoals);
    }
    
    private static double TeamMarkovChainProbability(IEnumerable<Game> pastGames, string team)
    {
        var goalsScored = new Dictionary<int, int>();
        var totalGames = 0;

        foreach (var match in pastGames)
        {
            if (match.Home != team && match.Away != team)
                continue;
            
            var goals = match.Home == team ? match.FullTimeHomeScore ?? 0 : match.FullTimeAwayScore ?? 0;

            if (goalsScored.ContainsKey(goals))
            {
                goalsScored[goals]++;
            }
            else
            {
                goalsScored[goals] = 1;
            }

            totalGames++;
        }

        var goalsScoredSum = goalsScored.Sum(goal => (double)(goal.Value * goal.Key) / totalGames);

        var probabilities = Enumerable.Range(0, 11)
            .Select(score =>
            {
                var probability = CalculatePoissonProbability(goalsScoredSum, score);
                return new MarkovChainResult(KeyBasedOnGoal(score), probability);
            })
            .GroupBy(p => p.Key)
            .Select(g => new MarkovChainResult(
                g.Key, 
                g.Sum(i => i.Probability)))
            .ToList();

        return probabilities.First(i => i.Key == "Score").Probability;
    }
    
    private static string KeyBasedOnGoal(int score)
    {
        var key = score switch
        {
            > 0 => "Score",
            0 => "NoScore",
            _ => throw new ArgumentOutOfRangeException(nameof(score), score, null)
        };
        
        return key;
    }
    
    private static double CalculatePoissonProbability(double lambda, int expectedGoal)
    {
        if (double.IsNaN(lambda) || lambda == 0)
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }

    private (int home, int away) CalculateModeBy(string home, string away, List<Game> games)
    {
        var homeGoal = games.Where(g => g.Home == home || g.Away == home)
            .Select(g => g.Home == home ? g.FullTimeHomeScore ?? 0 : g.FullTimeAwayScore ?? 0)
            .ToArray();

        var awayGoal = games.Where(g => g.Home == away || g.Away == away)
            .Select(g => g.Home == away ? g.FullTimeHomeScore ?? 0 : g.FullTimeAwayScore ?? 0)
            .ToArray();
        
        var homeMode = CalculateMode(homeGoal);
        var awayMode = CalculateMode(awayGoal);

        return (homeMode, awayMode);

    }

    private static int CalculateMode(IEnumerable<int> values)
    {
        var mode = 0;
        var maxCount = 0;
        var valueCounts = new Dictionary<int, int>();

        // Count the frequency of each value
        foreach (var value in values)
        {
            if (valueCounts.ContainsKey(value))
            {
                valueCounts[value]++;
            }
            else
            {
                valueCounts[value] = 1;
            }
        }

        // Find the value with the highest frequency
        foreach (var value in valueCounts.Keys.Where(value => valueCounts[value] > maxCount))
        {
            maxCount = valueCounts[value];
            mode = value;
        }

        return mode;
    }
}
using MathNet.Numerics.Distributions;
using PredictionTool.Extensions;
using PredictionTool.Interfaces;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class TeamStrengthCalculator: ITeamStrengthCalculator
{
    public TeamStrengthCalculator() { }
    
    /// <summary>
    /// This method will calculate all upcoming games Team strength based on which field they are playing with poisson distribution.
    /// To do that the current season games will be used
    /// </summary>
    public List<GameProbability> Calculate(List<Game> historicalGames, string homeTeam, string awayTeam, string league)
    {
        var result = new List<GameProbability>();
        
        var games = historicalGames.GetCurrentSeasonGamesBy(2022, 2023, league);
        var allGames = historicalGames.GetCurrentSeasonGamesBy(2016, 2022, league);
        
        var currentAnalyse = AnalysePerformance(homeTeam, awayTeam, league, games);
        var allGamesAnalyse = AnalysePerformance(homeTeam, awayTeam, league, allGames);


        var probabilities = new List<PoissonProbability>();
        foreach (var probability in currentAnalyse)
        {
            var allPoisonProbability = allGamesAnalyse
                .Where(i => i.Key == probability.Key)
                .Select(ii => ii.Probability)
                .FirstOrDefault();

            probabilities.Add(probability with
            {
                Probability = allPoisonProbability.CalculateWeighting(probability.Probability)
            });
        }

        var homeMarkovChain = TeamMarkovChainProbability(historicalGames, homeTeam);
        var awayMarkovChain = TeamMarkovChainProbability(historicalGames, awayTeam);
        var topProbability = probabilities
            .Where(i => i.Probability > 0.60)
            .ToList();

        if (topProbability.Count > 0)
        {
            var probability = topProbability.MaxBy(i => i.Probability);
            result.Add(new GameProbability(
                $"{homeTeam}:{awayTeam} - {probability.Key}",
                probability.Probability,
                homeMarkovChain,
                awayMarkovChain
            ));

         //   result.ForEach(i => Console.WriteLine($"{i}\t"));
        }
        
        return result;
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

    private static List<PoissonProbability> AnalysePerformance(
        string homeTeam, string awayTeam, string league, IList<Game> games)
    {
        // Retrieving the season of the league by year.
        if (!games.TeamsAreInLeague(homeTeam, awayTeam))
            return new List<PoissonProbability>();
        
        var homeMatches = CalculateTeamStrengthBy(games, homeTeam, league, true);
        var awayMatches = CalculateTeamStrengthBy(games, awayTeam, league);
            
        var expectedHomeGoal = homeMatches.TeamAttack * awayMatches.TeamDefense * homeMatches.LeagueScoredGoal;
        var expectedAwayGoal = awayMatches.TeamAttack * homeMatches.TeamDefense * homeMatches.LeagueConcededGaol;
        var probabilities = PossibleProbabilities(expectedHomeGoal, expectedAwayGoal);
        
        return probabilities;
    }
    
    public static List<PoissonProbability> PossibleProbabilities(double homeGoalAverage, double awayGoalAverage)
    {
        var probabilities = new List<PoissonProbability>();
        for (var homeScore = 0; homeScore <= 10; homeScore++)
        {
            for (var awayScore = 0; awayScore <= 10; awayScore++)
            {
                var homePoissonProbability = CalculatePoissonProbability(homeGoalAverage, homeScore);
                var awayPoissonProbability = CalculatePoissonProbability(awayGoalAverage, awayScore);
                var finalProbability = Math.Round(homePoissonProbability * awayPoissonProbability, 2);
                
                AddScoreProbabilities(probabilities, homeScore, awayScore, finalProbability);
            }
        }

        var result = probabilities
            .GroupBy(p => p.Key)
            .Select(g => new PoissonProbability(
                g.Key,g.Sum(i => i.Probability)))
            .ToList();
        
        return result;
    }

    private static void AddScoreProbabilities(ICollection<PoissonProbability> probabilities, int homeScore, int awayScore, double probability)
    {
        if (homeScore + awayScore > 2)
        {
            probabilities.Add(new PoissonProbability("MoreThanTwoGoals", probability));
        }
        if (homeScore > 0 && awayScore > 0)
        {
            probabilities.Add(new PoissonProbability("BothTeamScore", probability));
        }
        if (homeScore + awayScore == 3 || homeScore + awayScore == 2)
        {
            probabilities.Add(new PoissonProbability("TwoToThree", probability));
        }
        if (homeScore + awayScore < 3)
        {
            probabilities.Add(new PoissonProbability("LessThanTwoGoals", probability));
        }
    }

    private static double CalculatePoissonProbability(double lambda, int expectedGoal)
    {
        if (double.IsNaN(lambda) || lambda == 0)
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }

    private static TeamStrength CalculateTeamStrengthBy(
        IList<Game> games, string team, string league, bool atHome = false)
    {
        var currentTeamGames = games
            .Where(i => atHome ? i.Home == team : i.Away == team)
            .ToList();

        var leagueTeamGames = games
            .Where(i => i.League == league)
            .ToList();
        
        var leagueAvg = CalculateGoalAverage(leagueTeamGames, currentTeamGames.Count, atHome);
        var teamAvg =  CalculateGoalAverage(currentTeamGames, atHome: atHome);

        var attack = teamAvg.scoreAvg.Divide(leagueAvg.scoreAvg);
        var defense = teamAvg.concededAvg.Divide(leagueAvg.concededAvg);
        
        return new TeamStrength(
            attack, 
            defense, 
            leagueAvg.scoreAvg,
            leagueAvg.concededAvg
        );
    }
    
    
    /// <summary>
    /// Compute the average goal scored and conceded for given team.
    /// </summary>
    /// <param name="games">List of the past games</param>
    /// <param name="count">provide if the League score</param>
    /// <param name="atHome">provide if the League score</param>
    /// <returns></returns>
    private static (double scoreAvg, double concededAvg) CalculateGoalAverage(IList<Game> games, int count = 0, bool atHome = false)
    {
        var totalGames = count == 0 ? games.Count : count * games.NumberOfTeamsLeague();
        var averageScored = games
            .Sum(i => atHome ? i.FullTimeHomeScore ?? 0 : i.FullTimeAwayScore ?? 0)
            .Divide(totalGames);
        
        var averageConcededScored = games
            .Sum(i => atHome ? i.FullTimeAwayScore ?? 0 : i.FullTimeHomeScore ?? 0)
            .Divide(totalGames);
        
        var result = (averageScored, averageConcededScored);

        return result;
    }
}
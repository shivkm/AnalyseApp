using AnalyseApp.Extensions;
using AnalyseApp.Models;

namespace AnalyseApp.Services;

public class PoissonHandler: AnalyseHandler
{
    public override GameQualification HandleRequest(List<HistoricalGame> pastGames, GameQualification gameQualification, string homeTeam, string awayTeam)
    {
        var league = pastGames.Select(i => i.Div).First();
        
        var poissonProbabilities = Probability(pastGames, homeTeam, awayTeam, league);

        gameQualification.PoissonBothScoreProbability = poissonProbabilities
            .FirstOrDefault(i => i.Key == BothTeamScoreGoal)?.PoisonProbability ?? 0;
        gameQualification.PoissonMoreThanTwoGoalsProbability = poissonProbabilities
            .FirstOrDefault(i => i.Key == MoreThanTwoGoals)?.PoisonProbability ?? 0;
        gameQualification.PoissonLessThanThreeGoalsProbability = poissonProbabilities
            .FirstOrDefault(i => i.Key == LessThanTwoScore)?.PoisonProbability ?? 0;
        gameQualification.PoissonTwoToThreeGoalsProbability = poissonProbabilities
            .FirstOrDefault(i => i.Key == TwoToThreeScored)?.PoisonProbability ?? 0;
        
        base.HandleRequest(pastGames, gameQualification, homeTeam, awayTeam);

        return gameQualification;
    }
    
    private static List<TeamProbability> Probability(IList<HistoricalGame> pastGames, string homeTeam, string awayTeam, string league)
    {
        var homeMatches = CalculateTeamStrengthBy(pastGames, homeTeam, league, true);
        var awayMatches = CalculateTeamStrengthBy(pastGames, awayTeam, league);
        
        var expectedHomeGoal = homeMatches.Attack * awayMatches.Defense * homeMatches.LeagueScoredAverage;
        var expectedAwayGoal = awayMatches.Attack * homeMatches.Defense * homeMatches.LeagueConcededAverage;
        var probabilities = PoissonProbability(expectedHomeGoal, expectedAwayGoal);

        return probabilities;
    }
    
    private static List<TeamProbability> PoissonProbability(double homeGoalAverage, double awayGoalAverage)
    {
        var probabilities = new List<TeamProbability>();

        for (var homeScore = 0; homeScore <= 10; homeScore++)
        {
            for (var awayScore = 0; awayScore <= 10; awayScore++)
            {
                var homePoissonProbability = GetProbabilityBy(homeGoalAverage, homeScore);
                var awayPoissonProbability = GetProbabilityBy(awayGoalAverage, awayScore);

                AddProbability(homeScore, awayScore, probabilities, homePoissonProbability, awayPoissonProbability);
            }
        }

        var result = probabilities
            .GroupBy(p => p.Key)
            .Select(g => new TeamProbability
            {
                Key = g.Key,
                PoisonProbability = g.Sum(i => i.PoisonProbability)
            })
            .ToList();

        return result;
    }

    private static void AddProbability(
        int homeScore, int awayScore, 
        ICollection<TeamProbability> probabilities, 
        double homePoissonProbability, double awayPoissonProbability)
    {
        var key = "";
        if (homeScore + awayScore > 2)
        {
            key = MoreThanTwoGoals;
        }

        if (homeScore > 0 && awayScore > 0)
        {
            key = BothTeamScoreGoal;
        }

        if (homeScore + awayScore == 3 ||
            homeScore + awayScore == 2)
        {
            key = TwoToThreeScored;
        }

        if (homeScore == 0 && awayScore == 0 || homeScore + awayScore <= 2)
        {
            key = LessThanTwoScore;
        }

        if (key != "")
        {
            probabilities.Add(new TeamProbability
            {
                Key = key,
                PoisonProbability = homePoissonProbability * awayPoissonProbability
            });
        }
    }

    private static TeamStrength CalculateTeamStrengthBy(IList<HistoricalGame> gameData, string team, string league, bool atHome = false)
    {
        var currentTeamGames = gameData
            .Where(i => atHome ? i.HomeTeam == team : i.AwayTeam == team)
            .ToList();

        var leagueTeamGames = gameData
            .Where(i => i.Div == league)
            .ToList();
        
        var leagueGoalAverage = CalculateGoalAverage(leagueTeamGames, currentTeamGames.Count, atHome);
        var teamGoalAverage =  CalculateGoalAverage(currentTeamGames.ToList(), atHome: atHome);

        var attack = teamGoalAverage.Scored.Divide(leagueGoalAverage.Scored);
        var defense = teamGoalAverage.Conceded.Divide(leagueGoalAverage.Conceded);

        return new TeamStrength(
            attack, 
            defense, 
            teamGoalAverage.Scored,
            teamGoalAverage.Conceded,
            leagueGoalAverage.Scored, 
            leagueGoalAverage.Conceded);
    }
    
     
    /// <summary>
    /// Compute the average goal scored and conceded for given team.
    /// </summary>
    /// <param name="gameData">List of the past games</param>
    /// <param name="count">provide if the League score</param>
    /// <param name="atHome">provide if the League score</param>
    /// <returns></returns>
    private static GoalScoredAndConcededAverage CalculateGoalAverage(
        IList<HistoricalGame> gameData, int count = 0, bool atHome = false)
    {
        var averageScored = gameData.GetGoalScoreAverage(count, atHome: atHome);
        var averageConcededScored = gameData.GetGoalConcededAverage(count, atHome: atHome);
        
        var result = new GoalScoredAndConcededAverage(averageScored, averageConcededScored);
        
        return result;
    }
}
using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class PredictService: IPredictService
{
    private readonly List<Matches> _historicalMatches;
    
    public PredictService(IFileProcessor fileProcessor)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
    }
    
    public bool OverUnderPredictor(string home, string away)
    {
        var premierLeagueGames = _historicalMatches.GetCurrentSeasonGamesBy(
            "E0",
            new DateTime(2022, 8, 1),
            new DateTime(2023, 6, 30)
        );

        var teams = Enum.GetValues(typeof(PremierLeague)).Length;
        var rightGame = new List<int>();
        var wrongGames = new List<int>();
        for (var i = 0; i < teams; i++)
        {
            var correctResult = new List<int>();
            var wrongResult = new List<int>();
            var teamName = (PremierLeague)Enum.ToObject(typeof(PremierLeague), i);
            
            for (var j = 0; j <= 5; j++)
            {
                var selectedTeamMatch = premierLeagueGames
                    .Where(ii => ii.HomeTeam == teamName.ToString() || ii.AwayTeam == teamName.ToString())
                    .Skip(j)
                    .FirstOrDefault();

                if (selectedTeamMatch is null)
                    continue;

                var analysisGoalProbability = AnalyseGoalProbabilities(premierLeagueGames, selectedTeamMatch);
                var over = analysisGoalProbability > 0.68;
                
                if (selectedTeamMatch.FTAG + selectedTeamMatch.FTHG > 2 && over) correctResult.Add(1);
                if (selectedTeamMatch.FTAG + selectedTeamMatch.FTHG < 4 && !over) correctResult.Add(1);
                if ((selectedTeamMatch.FTAG + selectedTeamMatch.FTHG < 3 && over) ||
                    (selectedTeamMatch.FTAG + selectedTeamMatch.FTHG > 2 && !over)) wrongResult.Add(1);
            }

            // team predictions accuracy
            if (wrongResult.Count <= 1 && correctResult.Count >= 5)
            {
                rightGame.Add(1);
            }
            else
            {
                wrongGames.Add(1);
            }
        }
        
        Console.WriteLine($"Correct prediction: {rightGame.Sum()} out of {premierLeagueGames.Count}");
        return true;
    }

    public string OverUnderPredictionBy(string home, string away, string playedOn)
    {
        var premierLeagueGames = _historicalMatches.GetCurrentSeasonGamesBy(
            "E0",
            new DateTime(2022, 8, 1),
            new DateTime(2023, 6, 30)
        );
        
        var analysisGoalProbability = AnalyseGoalProbabilities(
            premierLeagueGames, new Matches { HomeTeam = home, AwayTeam = away, Date = playedOn }
        );
        
        var over = analysisGoalProbability > 0.65 ? "Over" : "Under";
        return over;
    }

    public bool TeamAnalysisBy()
    {
        // Sample data: List of premier league match objects for the last three seasons
        var lastThreeSeasonMatches = new List<List<Matches>>
        {
            _historicalMatches.Where(i => i.Div == "EO").ToList()
        };

        for (var season  = 0; season  < 3; season ++)
        {
            var teamsData = new Dictionary<string, TeamAnalysis>();
            foreach (var matches in lastThreeSeasonMatches[season])
            {
                // Process Home Team
                teamsData.TryAdd(matches.HomeTeam, new TeamAnalysis { TeamName = matches.HomeTeam });

                teamsData[matches.HomeTeam].AvgGoalsScored += (double)matches.FTHG;
                teamsData[matches.HomeTeam].AvgGoalsConceded += (double)matches.FTAG;
                
                // Process Away Team
                teamsData.TryAdd(matches.AwayTeam, new TeamAnalysis { TeamName = matches.AwayTeam });

                teamsData[matches.AwayTeam].AvgGoalsScored += (double)matches.FTAG;
                teamsData[matches.AwayTeam].AvgGoalsConceded += (double)matches.FTHG;

            }
            
            // Calculate average goals scored and conceded for each team
            foreach (var teamData in teamsData.Values)
            {
                var matchesCount = lastThreeSeasonMatches[season]
                    .Count(m => m.HomeTeam == teamData.TeamName || m.AwayTeam == teamData.TeamName);
                
                teamData.AvgGoalsScored /= matchesCount;
                teamData.AvgGoalsConceded /= matchesCount;
            }
        }

        return true;
    }

    
    private double AnalyseGoalProbabilities(List<Matches> premierLeagueGames, Matches selectedTeamMatch)
    {
        var homeTeam = selectedTeamMatch.HomeTeam;
        var awayTeam = selectedTeamMatch.AwayTeam;
        var playedOn = selectedTeamMatch.Date;

        var homeMatches = premierLeagueGames.GetMatchesBy(homeTeam, playedOn).ToList();
        var awayMatches = premierLeagueGames.GetMatchesBy(awayTeam, playedOn).ToList();
        var headToHeadMatches = _historicalMatches.GetHeadToHeadMatchesBy(homeTeam, awayTeam, playedOn).ToList();

        var overallProbability = GetSessionCalculatedAverageBy(homeTeam, awayTeam, homeMatches, awayMatches);
        var lastSixMatchesProbability = GetLastSixMatchesProbabilityBy(homeTeam, awayTeam, homeMatches, awayMatches);
        var lastSixSpecificProbability = GetLastSixSpecificFieldMatchesProbabilityBy(homeTeam, awayTeam, homeMatches, awayMatches);
        var headToHeadProbability = GetHeadToHeadMatchesProbabilityBy(homeTeam, awayTeam, headToHeadMatches);

        var value = (
            overallProbability * 0.30 + 
            lastSixMatchesProbability * 0.30 +
            headToHeadProbability * 0.40) * 0.50 + lastSixSpecificProbability * 0.50;
        
        return value;
    }
   
        private static double GetSessionCalculatedAverageBy(
        string homeTeam,
        string awayTeam,
        List<Matches> homeMatches, 
        List<Matches> awayMatches)
    {
        var homeScoreAvg = homeMatches.GetScoredGoalAverageBy(homeTeam);
        var homeConcededAvg = homeMatches.GetConcededGoalAverageBy(homeTeam);
        var awayScoreAvg = awayMatches.GetScoredGoalAverageBy(awayTeam);
        var awayConcededAvg = awayMatches.GetConcededGoalAverageBy(awayTeam);

        var homeLambda = (homeScoreAvg + homeConcededAvg) / homeScoreAvg;
        var awayLambda = (awayScoreAvg + awayConcededAvg) / awayScoreAvg;
        
        var probability = PoissonAnalysisBy(homeLambda, awayLambda).Sum();

        return probability;
    }
    
    private static double GetLastSixSpecificFieldMatchesProbabilityBy(
        string homeTeam,
        string awayTeam,
        List<Matches> homeMatches, 
        List<Matches> awayMatches)
    {
        homeMatches = homeMatches.Where(ii => ii.HomeTeam == homeTeam).Take(6).ToList();
        awayMatches = awayMatches.Where(ii => ii.AwayTeam == awayTeam).Take(6).ToList();
        
        var homeScoreAvg = homeMatches.GetHomeGoalScoredAverageBy(homeTeam);
        var homeConcededAvg = homeMatches.GetHomeGoalConcededAverageBy(homeTeam);
        var awayScoreAvg = awayMatches.GetAwayGoalScoredAverageBy(awayTeam);
        var awayConcededAvg = awayMatches.GetAwayGoalConcededAverageBy(awayTeam);

        var homeLambda = (homeScoreAvg + homeConcededAvg) / homeScoreAvg;
        var awayLambda = (awayScoreAvg + awayConcededAvg) / awayScoreAvg;
        
        var probability = PoissonAnalysisBy(homeLambda, awayLambda).Sum();

        return probability;
    }
    
    private static double GetLastSixMatchesProbabilityBy(
        string homeTeam,
        string awayTeam,
        List<Matches> homeMatches, 
        List<Matches> awayMatches)
    {
        homeMatches = homeMatches.Take(6).ToList();
        awayMatches = awayMatches.Take(6).ToList();
        
        var homeScoreAvg = homeMatches.GetScoredGoalAverageBy(homeTeam);
        var homeConcededAvg = homeMatches.GetConcededGoalAverageBy(homeTeam);
        var awayScoreAvg = awayMatches.GetScoredGoalAverageBy(awayTeam);
        var awayConcededAvg = awayMatches.GetConcededGoalAverageBy(awayTeam);

        var homeLambda = (homeScoreAvg + homeConcededAvg) / homeScoreAvg;
        var awayLambda = (awayScoreAvg + awayConcededAvg) / awayScoreAvg;
        
        var probability = PoissonAnalysisBy(homeLambda, awayLambda).Sum();

        return probability;
    }
    
    
    private static double GetHeadToHeadMatchesProbabilityBy(
        string homeTeam,
        string awayTeam,
        List<Matches> headToHeadMatches)
    {
        // Overall
        var homeOverallScoreAvg = headToHeadMatches.GetScoredGoalAverageBy(homeTeam);
        var homeOverallConcededAvg = headToHeadMatches.GetConcededGoalAverageBy(homeTeam);
        
        var awayOverallScoreAvg = headToHeadMatches.GetScoredGoalAverageBy(awayTeam);
        var awayOverallConcededAvg = headToHeadMatches.GetConcededGoalAverageBy(awayTeam);

        var homeOverallLambda = (homeOverallScoreAvg + homeOverallConcededAvg) / homeOverallScoreAvg;
        var awayOverallLambda = (awayOverallScoreAvg + awayOverallConcededAvg) / awayOverallScoreAvg;
        var overallProbability = PoissonAnalysisBy(homeOverallLambda, awayOverallLambda).Sum();
        
        // Specific Field
        var homeScoredGoalAvg = headToHeadMatches.GetHomeGoalScoredAverageBy(homeTeam);
        var homeConcededGoalAvg = headToHeadMatches.GetHomeGoalScoredAverageBy(homeTeam);
        
        var awayScoredGoalAvg = headToHeadMatches.GetAwayGoalScoredAverageBy(awayTeam);
        var awayConcededGoalAvg = headToHeadMatches.GetAwayGoalConcededAverageBy(awayTeam);

        var homeLambda = (homeScoredGoalAvg + homeConcededGoalAvg) / homeScoredGoalAvg;
        var awayLambda = (awayScoredGoalAvg + awayConcededGoalAvg) / awayScoredGoalAvg;
        var probability = PoissonAnalysisBy(homeLambda, awayLambda).Sum();


        if (double.IsNaN(probability))
            probability = 0;
        
        if (double.IsNaN(overallProbability))
            overallProbability = 0;
        
        var value =  probability * 0.65 + overallProbability * 0.35;
        
        return value;
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
                    case false when (awayGoals is 0 && homeGoals is 0) ||
                                    (awayGoals is 0 && homeGoals is 1 or 2) ||
                                    (homeGoals is 0 && awayGoals is 1 or 2):
                        probability.Add(probHomeGoals * probAwayGoals);
                        break;
                }
            }
        }

        return probability;
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


public static class SoccerMatchPredictor
{
    public static (int homeGoals, int awayGoals) PredictMatchScore(double homeTeamGoalsScoredAvg, double homeTeamGoalsConcededAvg,
        double awayTeamGoalsScoredAvg, double awayTeamGoalsConcededAvg)
    {
        var homeGoalsProbabilities = CalculatePoissonProbabilities(homeTeamGoalsScoredAvg, awayTeamGoalsConcededAvg);
        var awayGoalsProbabilities = CalculatePoissonProbabilities(awayTeamGoalsScoredAvg, homeTeamGoalsConcededAvg);

        var matchProbabilities = new double[11, 11]; // Assuming we're considering up to 10 goals for each team

        for (int homeGoals = 0; homeGoals <= 10; homeGoals++)
        {
            for (int awayGoals = 0; awayGoals <= 10; awayGoals++)
            {
                matchProbabilities[homeGoals, awayGoals] = homeGoalsProbabilities[homeGoals] * awayGoalsProbabilities[awayGoals];
            }
        }

        int predictedHomeGoals = 0, predictedAwayGoals = 0;
        double highestProbability = 0;

        for (int homeGoals = 0; homeGoals <= 10; homeGoals++)
        {
            for (int awayGoals = 0; awayGoals <= 10; awayGoals++)
            {
                if (matchProbabilities[homeGoals, awayGoals] > highestProbability)
                {
                    highestProbability = matchProbabilities[homeGoals, awayGoals];
                    predictedHomeGoals = homeGoals;
                    predictedAwayGoals = awayGoals;
                }
            }
        }

        return (predictedHomeGoals, predictedAwayGoals);
    }

    private static double[] CalculatePoissonProbabilities(double lambda, double opponentLambda)
    {
        const int maxGoals = 10;
        var probabilities = new double[maxGoals + 1];

        for (int goals = 0; goals <= maxGoals; goals++)
        {
            probabilities[goals] = PoissonProbability(lambda, goals) * PoissonProbability(opponentLambda, goals);
        }

        return probabilities;
    }

    private static double PoissonProbability(double lambda, int k)
    {
        return Math.Pow(lambda, k) * Math.Exp(-lambda) / Factorial(k);
    }

    private static double Factorial(int n)
    {
        if (n <= 1)
            return 1;

        return n * Factorial(n - 1);
    }
}

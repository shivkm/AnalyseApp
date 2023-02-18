using AnalyseApp.Algorithm;
using AnalyseApp.Extensions;
using AnalyseApp.Generics;
using AnalyseApp.Models;
using MathNet.Numerics.Distributions;

namespace AnalyseApp.Services;


public class CalculateService
{
    private const string ZeroGoal = "ZeroGoal";
    private const string AtLeastOneGoal = "atLeastOneGoal";
    private const string MoreThanOneGoal = "MoreThanOneGoal";
    private const string BothTeamScoreGoal = "BothTeamScoreGoal";
    private const string MoreThanTwoGoals = "MoreThanTwoGoals";
    private const double PassingPercentage = 0.60;
    private const double Alpha = 0.5;
    
    private List<MarkovChainResult> _currentHomeMarkovChain = new();
    private List<MarkovChainResult> _currentAwayMarkovChain = new();
    private List<MarkovChainResult> _headToHeadMarkovChain = new();
    private List<SelfPoisonResult> _currentHomPoison = new();
    private List<SelfPoisonResult> _currentAwayPoison = new();
    private List<SelfPoisonResult> _headToHeadPoison = new();
    private string _message = string.Empty;
    private bool _qualified;

    private readonly IList<HistoricalGame> _currentSeasonPastGames;
    private readonly IList<HistoricalGame> _PastGames;
    private readonly MarkovChain _markovChain;

    public CalculateService(IReadOnlyCollection<HistoricalGame> pastGames)
    {
        _currentSeasonPastGames = pastGames.GetGameDataBy(2022, 2023);
        _PastGames = pastGames.GetGameDataBy(2016, 2023);
        _markovChain = new MarkovChain();
    }

    /// <summary>
    /// This method will use the markov chain to calculate the average and give to the poison to generate probability.
    /// It also calculate the goal and score average and give that average to the poison to generate per team score and conceded goal probability.
    /// Furthermore it calculate the monte carlo probability as well. 
    /// </summary>
    /// <param name="home">Team name playing at Home</param>
    /// <param name="away">Team name playing at Away</param>
    /// <returns>The class itself so the chain can be used.</returns>
    public CalculateService CalculateProbabilitiesBy(string home, string away)
    {
        _currentHomeMarkovChain = TeamMarkovChainProbability(_currentSeasonPastGames, home);
        _currentAwayMarkovChain = TeamMarkovChainProbability(_currentSeasonPastGames, away);
        _currentHomPoison = TeamPoisonProbability(_currentSeasonPastGames, home);
        _currentAwayPoison = TeamPoisonProbability(_currentSeasonPastGames, away);

        _headToHeadMarkovChain = HeadToHeadMarkovChainProbability(_PastGames, home, away);
        _headToHeadPoison = HeadToHeadPoisonProbability(_PastGames, home, away);

        return this;
    }

    public CalculateService FilterCriticalCases(string home, string away)
    {
        _message = string.Empty;
        _qualified = false;
        var lastFourGamesOver = LastFourGamesOverTwoGoals(home, away);
        var lastTwoZeroZeroGame = LastTwoGamesZeroZero(home, away);
        if (lastFourGamesOver)
        {
            _message = "Both have last four games over.";
        }
        if (lastTwoZeroZeroGame)
        {
            _message = "Both have last two games 0:0.";
        }

        var oneSideGoalGames = OneSideResultGamesInPastTenGames(home, away);
        var headToHeadOneSide = OneSideResultGamesInHeadToHeadGames(home, away);
        if (oneSideGoalGames.Home > 0.35 || oneSideGoalGames.Away > 0.35 || headToHeadOneSide > 0.35)
        {
            var team = oneSideGoalGames.Home > 0.35 ? "Home" : "Away";
            _message = $"{team} has more than 30% one side result.";
        }
        
        var zeroZeroGames = ZeroZeroGamesInPastTenGames(home, away);
        var headToHeadZeroZero = ZeroZeroGamesInHeadToHeadGames(home, away);
        
        if (zeroZeroGames.Home > 0.30 || zeroZeroGames.Away > 0.30 || headToHeadZeroZero > 0.30)
        {
            var team = oneSideGoalGames.Home > 0.30 ? "Home" : "Away";
            _message = $"{team} has more than 30% one side result.";
        }
        
        return this;
    }
    
    public void BothScoreGames(string home, string away)
    {
        // Select the AtLeastOneGoal property to filter the passing stage
        var currentHomeMarkovChain  = _currentHomeMarkovChain
            .First(i => i.Key == AtLeastOneGoal);
        
        var currentAwayMarkovChain  = _currentAwayMarkovChain
            .First(i => i.Key == AtLeastOneGoal);
        
        var currentHomePoisonAndMonteCarlo  = _currentHomPoison
            .First(i => i.Key == AtLeastOneGoal);
        
        var currentAwayPoisonAndMonteCarlo  = _currentAwayPoison
            .First(i => i.Key == AtLeastOneGoal);

        if (currentHomeMarkovChain.Probability > 0.60 &&
            currentAwayMarkovChain.Probability > 0.60)
        {
            Console.WriteLine($"MarkovChain Poison Pass: {home}{currentHomeMarkovChain.Probability}%:{away}{currentAwayMarkovChain.Probability}%");
        }
        var finalHomeProbability = (1 - Alpha) * currentHomeMarkovChain.Probability +
                                       Alpha * currentHomePoisonAndMonteCarlo.MonteCarlo +
                                       Alpha * currentHomePoisonAndMonteCarlo.ScoreProbability +
                                       Alpha * currentHomePoisonAndMonteCarlo.ConcededProbability;
        
        var normalizingFactor = 1 - Alpha + Alpha * currentHomePoisonAndMonteCarlo.MonteCarlo + 
                                Alpha * currentHomePoisonAndMonteCarlo.ScoreProbability + 
                                Alpha * currentHomePoisonAndMonteCarlo.ConcededProbability;

        finalHomeProbability = finalHomeProbability / normalizingFactor;
        var finalAwayProbability = (1 - Alpha) * currentAwayMarkovChain.Probability +
                                   Alpha * currentAwayPoisonAndMonteCarlo.MonteCarlo +
                                   Alpha * currentAwayPoisonAndMonteCarlo.ScoreProbability +
                                   Alpha * currentAwayPoisonAndMonteCarlo.ConcededProbability;
        
        
        var normalizingAwayFactor = 1 - Alpha + Alpha * currentAwayPoisonAndMonteCarlo.MonteCarlo + 
                                Alpha * currentAwayPoisonAndMonteCarlo.ScoreProbability + 
                                Alpha * currentAwayPoisonAndMonteCarlo.ConcededProbability;

        finalAwayProbability = finalAwayProbability / normalizingAwayFactor;

        if (finalHomeProbability > 0.70 && finalAwayProbability > 0.70)
        {
            
        }
        if (HeadToHeadQualified() &&
            currentHomeMarkovChain.Probability > PassingPercentage &&
            currentAwayMarkovChain.Probability > PassingPercentage &&
            currentHomePoisonAndMonteCarlo is { ScoreProbability: > PassingPercentage, MonteCarlo: > PassingPercentage } &&
            currentAwayPoisonAndMonteCarlo is { ScoreProbability: > PassingPercentage, MonteCarlo: > PassingPercentage })
        {
            if (currentHomePoisonAndMonteCarlo.ConcededProbability > PassingPercentage &&
                currentAwayPoisonAndMonteCarlo.ConcededProbability > PassingPercentage)
            {
                _qualified = true;
            }
        }
    }

    /// <summary>
    /// Filter the score probability of poison and monte carlo. If the all probability pass the passing percentage
    /// then it will return true
    /// </summary>
    /// <returns>True if probabilities are bigger than passing percentage.</returns>
    private bool HeadToHeadQualified()
    {
        var headToHeadMarkovChain  = _headToHeadMarkovChain
            .First(i => i.Key == BothTeamScoreGoal);
        
        var headToHeadPoisonAndMonteCarlo  = _headToHeadPoison
            .First(i => i.Key == BothTeamScoreGoal);

        return headToHeadPoisonAndMonteCarlo is
        {
            // Home score poison probability
            ScoreProbability: > PassingPercentage,
            // Away score poison probability
            ConcededProbability: > PassingPercentage,
            // Home monte carlo probability
            MonteCarlo: > PassingPercentage,
            // Away monte carlo probability
            AwayMonteCarlo: > PassingPercentage
            // markov chain probability for both score
        } && headToHeadMarkovChain.Probability > 0.30;
    }
    
    private List<MarkovChainResult> HeadToHeadMarkovChainProbability(IList<HistoricalGame> pastGames, string homeTeam, string awayTeam)
    {
        var games = pastGames
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                                i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .ToList();
        
        _markovChain.AddGame(games);
        
        var scoreAverage = _markovChain.PredictScore(homeTeam, awayTeam);
        var probabilities = new List<MarkovChainResult>();

        for (var score = 0; score <= 3; score++)
        {
            var homeTeamProbability = Poisson(scoreAverage.Item1, score);
            var awayTeamProbability = Poisson(scoreAverage.Item2, score);
            var probability = homeTeamProbability * 0.50 + awayTeamProbability * 0.50;
            probabilities.Add(new MarkovChainResult(KeyBasedOnGoal(score, score, true), probability));
        }

        var result = probabilities
            .GroupBy(p => p.Key)
            .Select(g => new MarkovChainResult(
                g.Key, 
                g.Sum(i => i.Probability))
            ).ToList();

        return result;
    }

    private List<MarkovChainResult> TeamMarkovChainProbability(IList<HistoricalGame> pastGames, string team)
    {
        var homeTeamGames = pastGames
            .Where(i => i.HomeTeam == team)
            .ToList();
        
        var awayTeamGames = pastGames
            .Where(i => i.AwayTeam == team)
            .ToList();
        
        _markovChain.AddHomeGamesBy(homeTeamGames);
        _markovChain.AddAwayGamesBy(awayTeamGames);
        
        var scoreAverage = _markovChain.PredictScore(team);
        var probabilities = new List<MarkovChainResult>();

        for (var score = 0; score <= 10; score++)
        {
            var probability = Poisson(scoreAverage, score);
            probabilities.Add(new MarkovChainResult(KeyBasedOnGoal(score), probability));
        }

        var result = probabilities
            .GroupBy(p => p.Key)
            .Select(g => new MarkovChainResult(
                g.Key, 
                g.Sum(i => i.Probability))
            ).ToList();

        return result;
    }

    private List<SelfPoisonResult> TeamPoisonProbability(IList<HistoricalGame> pastGames, string team)
    {
        var homeGames = pastGames
            .Where(i => i.HomeTeam == team)
            .ToList();

        var awayGames = pastGames
            .Where(i => i.AwayTeam == team)
            .ToList();
        
        var countValue = homeGames.Count + awayGames.Count;
        var totalHomeGoals = homeGames.Sum(m => m.FTHG ?? 0);
        var totalAwayGoals = awayGames.Sum(m => m.FTAG ?? 0);
        var scoreAverage = (totalHomeGoals + totalAwayGoals).Divide(countValue);
        
        var totalHomeConceded = homeGames.Sum(m => m.FTAG ?? 0);
        var totalAwayConceded = awayGames.Sum(m => m.FTHG ?? 0);
        var concededAverage = (totalHomeConceded + totalAwayConceded).Divide(countValue);

        var probabilities = new List<SelfPoisonResult>();

        for (var score = 0; score <= 10; score++)
        {
            var scoreProbability = Poisson(scoreAverage, score);
            var concededProbability = Poisson(concededAverage, score);
            probabilities.Add(new SelfPoisonResult(KeyBasedOnGoal(score), scoreProbability, concededProbability));
        }

        var result = probabilities
            .GroupBy(p => p.Key)
            .Select(g => new SelfPoisonResult(
                g.Key, 
                g.Sum(i => i.ScoreProbability),
                g.Sum(i => i.ConcededProbability))
                {
                    MonteCarlo = MonteCarlo(scoreAverage)
                }
                
            ).ToList();

        return result;
    }
    
    private List<SelfPoisonResult> HeadToHeadPoisonProbability(IList<HistoricalGame> pastGames, string homeTeam, string awayTeam)
    {
        var games = pastGames
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                        i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .ToList();
        
        var countValue = games.Count;
        var totalHomeGoals = games.Sum(m => m.FTHG ?? 0);
        var homeScoreAverage = totalHomeGoals.Divide(countValue);
        
        var totalAwayGoals = games.Sum(m => m.FTAG ?? 0);
        var awayScoreAverage = totalAwayGoals.Divide(countValue);
        
        var probabilities = new List<SelfPoisonResult>();

        for (var score = 0; score <= 10; score++)
        {
            var scoreProbability = Poisson(homeScoreAverage, score);
            var concededProbability = Poisson(awayScoreAverage, score);
            probabilities.Add(new SelfPoisonResult(KeyBasedOnGoal(score, score, true), scoreProbability, concededProbability));
        }

        var result = probabilities
            .GroupBy(p => p.Key)
            .Select(g => new SelfPoisonResult(
                    g.Key, 
                    g.Sum(i => i.ScoreProbability),
                    g.Sum(i => i.ConcededProbability))
                {
                    MonteCarlo = MonteCarlo(homeScoreAverage),
                    AwayMonteCarlo = MonteCarlo(awayScoreAverage)
                }
                
            ).ToList();

        return result;
    }
    
    private static string KeyBasedOnGoal(int score, int awayTeamScore = 0, bool headToHead = false)
    {
        if (headToHead)
        {
            if (score > 0 && awayTeamScore > 0)
                return BothTeamScoreGoal;
            
            if (score + awayTeamScore > 2)
                return MoreThanTwoGoals;
            
        }
        var key = score switch
        {
            > 0 => AtLeastOneGoal,
            0 => ZeroGoal,
            _ => MoreThanOneGoal
        };
        
        return key;
    }
    
    private static double Poisson(double lambda, int expectedGoal)
    {
        if (double.IsNaN(lambda) || lambda == 0)
            return 0;

        var poisson = new Poisson(lambda);
        var probability = poisson.Probability(expectedGoal);

        return probability;
    }
    
    private static double MonteCarlo(double scoreAverage)
    {
        const int simulationIterations = 10000;
        var goalsScored  = 0;

        for (var i = 0; i < simulationIterations; i++)
        {
            var homeTeamScores = SimulateScore(scoreAverage);

            if (homeTeamScores)
            {
                goalsScored++;
            }
        }

        var bothScoreProbability = (double)goalsScored / simulationIterations;

        return bothScoreProbability;
    }

    private bool LastFourGamesOverTwoGoals(string home, string away)
    {
        // Home team matches
        var homeHomeGames = _currentSeasonPastGames
            .Where(i => i.HomeTeam == home).Take(2).ToList();
        var homeAwayGames = _currentSeasonPastGames
            .Where(i => i.AwayTeam == home).Take(2).ToList();

        // Away team matches
        var awayHomeGames = _currentSeasonPastGames
            .Where(i => i.HomeTeam == away).Take(2).ToList();
        var awayAwayGames = _currentSeasonPastGames
            .Where(i => i.AwayTeam == away).Take(2).ToList();
        
        if (homeHomeGames.All(i => i.FTHG > 2) && homeAwayGames.All(i => i.FTAG > 2) &&
            awayHomeGames.All(i => i.FTHG > 2) && awayAwayGames.All(i => i.FTAG > 2))
        {
            return true;
        }

        return false;
    }

    private bool LastTwoGamesZeroZero(string home, string away)
    {
        var homeHomeGames = _currentSeasonPastGames
            .Where(i => i.HomeTeam == home).Take(1).ToList();
        var homeAwayGames = _currentSeasonPastGames
            .Where(i => i.AwayTeam == home).Take(1).ToList();

        // Away team matches
        var awayHomeGames = _currentSeasonPastGames
            .Where(i => i.HomeTeam == away).Take(1).ToList();
        var awayAwayGames = _currentSeasonPastGames
            .Where(i => i.AwayTeam == away).Take(1).ToList();
        
        if (homeHomeGames.All(i => i.FTHG + i.FTAG  == 0) &&
            homeAwayGames.All(i => i.FTHG + i.FTAG == 0) &&
            awayHomeGames.All(i => i.FTHG + i.FTAG  == 0) &&
            awayAwayGames.All(i => i.FTHG + i.FTAG == 0))
        {
            return true;
        }

        return false;
    }

    private double ZeroZeroGamesInHeadToHeadGames(string home, string away)
    {
        var games = _PastGames
            .Where(i => i.HomeTeam == home && i.AwayTeam == away ||
                                    i.HomeTeam == away && i.AwayTeam == home)
            .ToList();


        var average = games.Count(i => i.FTHG + i.FTAG == 0).Divide(games.Count);

        return average;
    }

    private double OneSideResultGamesInHeadToHeadGames(string home, string away)
    {
        var games = _PastGames
            .Where(i => i.HomeTeam == home && i.AwayTeam == away ||
                        i.HomeTeam == away && i.AwayTeam == home)
            .ToList();


        var average =  games
            .Count(i => i.FTHG + i.FTAG < 3 && i is { FTHG: > 0, FTAG: 0 } or { FTHG: 0, FTAG: > 0 })
            .Divide(games.Count);

        return average;
    }
    
    private (double Home, double Away) ZeroZeroGamesInPastTenGames(string home, string away)
    {
        var homeGames = _currentSeasonPastGames
            .Where(i => i.HomeTeam == home || i.AwayTeam == away).Take(10).ToList();

        var awayGames = _currentSeasonPastGames
            .Where(i => i.HomeTeam == away || i.AwayTeam == away).Take(10).ToList();

        var homeAverage = homeGames.Count(i => i.FTHG + i.FTAG == 0).Divide(homeGames.Count);
        var awayAverage = awayGames.Count(i => i.FTHG + i.FTAG == 0).Divide(awayGames.Count);

        return (homeAverage, awayAverage);
    }

    private (double Home, double Away) OneSideResultGamesInPastTenGames(string home, string away)
    {
        var homeGames = _currentSeasonPastGames
            .Where(i => i.HomeTeam == home || i.AwayTeam == home).Take(10).ToList();

        var awayGames = _currentSeasonPastGames
            .Where(i => i.HomeTeam == away || i.AwayTeam == away).Take(10).ToList();

        var homeAverage = homeGames
            .Count(i => i.FTHG + i.FTAG < 3 && i is { FTHG: > 0, FTAG: 0 } or { FTHG: 0, FTAG: > 0 })
            .Divide(homeGames.Count);
        
        var awayAverage = awayGames
            .Count(i => i.FTHG + i.FTAG < 3 && i is { FTHG: > 0, FTAG: 0 } or { FTHG: 0, FTAG: > 0 })
            .Divide(awayGames.Count);

        return (homeAverage, awayAverage);
    }











    
    private static (double Over, double Unter) MonteCarloOverLessProbability(double goalsThreshold)
    {
        const int numSimulations = 10000;
        var overCount = 0;
        var underCount = 0;
        var random = new Random();

        for (var i = 0; i < numSimulations; i++)
        {
            var goals = random.NextGaussian(goalsThreshold, 0.5);
            if (goals > goalsThreshold)
            {
                overCount++;
            }
            else
            {
                underCount++;
            }
        }

        var overProb = (double)overCount / numSimulations;
        var underProb = (double)underCount / numSimulations;

        return (overProb, underProb);
    }
    
    private static (double Over, double Unter) MarkovChainOverLessProbability(double goalsThreshold)
    {
        const int numSimulations = 10000;
        // Define the states for the Markov Chain
        const int UNDER = 0;
        const int OVER = 1;

        // Define the transition matrix for the Markov Chain
        double[,] transitionMatrix = 
        {
            {0.6, 0.4},
            {0.5, 0.5}
        };
        
        // Define the initial probabilities
        double[] initialProbabilities = 
        {
            goalsThreshold,
            1.0 - goalsThreshold
        };
        // Run the Markov Chain simulation for 10 steps
        const int steps = 10;
        var currentProbabilities = initialProbabilities;
        for (var i = 0; i < steps; i++)
        {
            var nextProbabilities = new double[2];
            for (var j = 0; j < 2; j++)
            {
                nextProbabilities[j] = 0;
                for (var k = 0; k < 2; k++)
                {
                    nextProbabilities[j] += currentProbabilities[k] * transitionMatrix[k, j];
                }
            }
            currentProbabilities = nextProbabilities;
        }
        return (currentProbabilities[OVER], currentProbabilities[UNDER]);
    }
    
    private static bool SimulateScore(double accuracy)
    {
        var random = new Random();
        return random.NextDouble() < accuracy;
    }

    private static double GetPassingLimitBy(string league, int betType = 0)
    {
        const double defaultLimit = 0.68;
        var championship = defaultLimit;
        var serieA = defaultLimit;
        var premierLeagueBundesliga = defaultLimit;
        var frenchLeague = defaultLimit;
        
        if (betType == 0)
        {
            championship = 0.60;
            premierLeagueBundesliga = 0.62;
            frenchLeague = 0.62;
        }
        if (betType == 1)
        {
            championship = 0.60;
            premierLeagueBundesliga = 0.64;
            frenchLeague = 0.64;
            serieA = 0.64;
        }
        if (betType == 2)
        {
            championship = 0.58;
            premierLeagueBundesliga = 0.58;
            frenchLeague = 0.58;
            serieA = 0.58;
        }
        if (betType == 3)
        {
            championship = 0.70;
            premierLeagueBundesliga = 0.70;
            frenchLeague = 0.70;
            serieA = 0.70;
        }
        switch (league)
        {
            // Championship and Serie A
            case "E1":
                return championship;
            case "I1":
                return serieA;
            // Premier und Bundesliga
            case "E0":
            case "D1":
            case "D2":
                return premierLeagueBundesliga;
            case "F1":
                return frenchLeague;
            default:
                return defaultLimit;
        }
    }
    
    
    
    
    

public record GameData
{
    public string Team { get; set; }
    public string AwayTeam { get; set; }
    public int FullTimeScore { get; set; }
    public int FullTimeScoreConceded { get; set; }
    public int HalftimeScore { get; set; }
    public int HalftimeScoreConceded { get; set; }
    public int Shots { get; set; }
    public int ShotsOnGoal { get; set; }
    public int AwayShots { get; set; }
    public int AwayShotsOnGoal { get; set; }
    public int Offsides { get; set; }
    public int AwayOffsides { get; set; }
}

public class MarkovChainService
{
    private readonly IList<HistoricalGame> _currentSeason;
    private readonly IList<HistoricalGame> _lastSixSeason;
    private List<GameData> _HeadToHeadData = new ();

    public MarkovChainService(IReadOnlyCollection<HistoricalGame> gameData)
    {
        _currentSeason = gameData.GetGameDataBy(2022, 2023);
        _lastSixSeason = gameData.GetGameDataBy(2016, 2022);
    }

    public void Execute(string homeTeam, string awayTeam)
    {
        var homeTeamData = GetListOfGameData(
            _currentSeason.Where(item => item.HomeTeam == homeTeam), true);
        
        var awayTeamData = GetListOfGameData(
            _currentSeason.Where(item => item.AwayTeam == awayTeam));
        
        // Create a Markov chain for each team
        var homeTeamMarkovChain = new MarkovChain<int>();
        var awayTeamMarkovChain = new MarkovChain<int>();
        
        // Train the Markov chain for each team using historical game data
        foreach (var data in homeTeamData)
        {
            homeTeamMarkovChain.Train(data.Shots, data.FullTimeScore);
            homeTeamMarkovChain.Train(data.Offsides, data.HalftimeScore);
            homeTeamMarkovChain.Train(data.FullTimeScore, data.FullTimeScoreConceded);
            homeTeamMarkovChain.Train(data.HalftimeScore, data.FullTimeScoreConceded);
            homeTeamMarkovChain.Train(data.FullTimeScoreConceded, data.Shots);
        }

        foreach (var data in awayTeamData)
        {
            awayTeamMarkovChain.Train(data.Shots, data.FullTimeScore);
            awayTeamMarkovChain.Train(data.Offsides, data.HalftimeScore);
            awayTeamMarkovChain.Train(data.FullTimeScore, data.FullTimeScoreConceded);
            awayTeamMarkovChain.Train(data.HalftimeScore, data.FullTimeScoreConceded);
            awayTeamMarkovChain.Train(data.FullTimeScoreConceded, data.Shots);
        }
        

        // Use the trained Markov chain to make a prediction for each team
        Console.WriteLine("Next state for team A: " + homeTeamMarkovChain.Predict(1));
        Console.WriteLine("Next state for team B: " + awayTeamMarkovChain.Predict(1));

    }


    private void TZest(string homeTeam, string awayTeam)
    {
        
    }
    
    private static List<GameData> GetListOfGameData(IEnumerable<HistoricalGame> historicalGames, bool isHome = false)
    {
        var gameData = historicalGames.Select(s => new GameData
        {
            Team = isHome ? s.HomeTeam : s.AwayTeam,
            FullTimeScore = isHome ? s.FTHG ?? 0: s.FTAG ?? 0,
            FullTimeScoreConceded = isHome ? s.FTAG ?? 0: s.FTHG ?? 0,
            HalftimeScore = isHome ? s.HTHG ?? 0: s.HTAG ?? 0,
            HalftimeScoreConceded = isHome ? s.HTAG ?? 0: s.HTHG ?? 0,
            Shots = isHome ? s.HS ?? 0: s.AS ?? 0,
            ShotsOnGoal = isHome ? s.HST ?? 0: s.AST ?? 0,
            Offsides = isHome ? s.HO ?? 0: s.AO ?? 0,

        }).ToList();

        return gameData;
    }
    
    
}
}
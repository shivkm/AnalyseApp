using PredictionTool.Interfaces;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class AnalyseService: IAnalyseService
{
    private readonly IFileProcessor _fileProcessor;
    private readonly ICalculatorService _calculatorService;
    private readonly IFilterService _filterService;
    
    private const double MinScored = 0.60;
    private const double MaxConceded = 1.20;
    private const double MinProbability = 0.68;

    public AnalyseService(
        IFileProcessor fileProcessor, IFilterService filterService,
        ICalculatorService calculatorService)
    {
        _fileProcessor = fileProcessor;
        _filterService = filterService;
        _calculatorService = calculatorService;
    }

    public async Task StartAnalyseAsync()
    {
        // Change this to load data backward for test
        var historicalEnd = new DateTime(2023, 02, 22);
        var endDate = DateTime.Now;
        //await _fileProcessor.CreateUpcomingFixtureBy(default);
        //await _fileProcessor.CreateHistoricalGamesFile(default);
        var historicalGames = _fileProcessor.GetHistoricalGamesBy(endDate);
        var upcomingGames = _fileProcessor.GetUpcomingGamesBy(endDate);
        var result = new List<QualifiedGames>();

        var teamFormCalculator = new TeamFormCalculator(historicalGames);

        foreach (var upcomingGame in upcomingGames)
        {
            // overall form of the teams
            var homeOverall = teamFormCalculator.CalculateForm(upcomingGame.Home, 1000);
            var awayOverall = teamFormCalculator.CalculateForm(upcomingGame.Away, 1000);
            
            // current form of the teams
            var home = teamFormCalculator.CalculateForm(upcomingGame.Home, 6);
            var away = teamFormCalculator.CalculateForm(upcomingGame.Away, 6);
            
            AnalyzeTactics(upcomingGame.Home, upcomingGame.Away, home,away);
            var goal = PredictBothTeamsToScore(home, away);
        }
        
        //Console.WriteLine($"count: {result.Count}");
       // result.ForEach(i => Console.WriteLine($"{i}\t"));
    }
    public void AnalyzeTactics(string homeTeam, string awayTeam, TeamForm homeForm, TeamForm awayForm)
    {
        var homeScore = homeForm.GoalProbability * awayForm.ConcededProbability * homeForm.GoalsForPerGame;
        var awayScore = awayForm.GoalProbability * homeForm.ConcededProbability * awayForm.GoalsForPerGame;

        //Console.WriteLine($"{homeTeam} is expected to score {homeScore:0.00} goals.");
        //Console.WriteLine($"{awayTeam} is expected to score {awayScore:0.00} goals.");

        if (homeScore > awayScore && homeScore > 0.85)
        {
           // Console.WriteLine($"{homeTeam} is favored to win.");
        }
        else if (homeScore < awayScore && awayScore > 0.85)
        {
            //Console.WriteLine($"{awayTeam} is favored to win.");
        }
        else if(homeScore < 0.30 && awayScore < 0.30)
        {
            Console.WriteLine($"{homeTeam}:{awayTeam}The match is expected to end in a draw.");
        }
    }
    
    public bool PredictBothTeamsToScore(TeamForm homeTeam, TeamForm awayTeam)
    {
        // Calculate the expected number of goals for each team
        double homeGoals = homeTeam.GoalsForPerGame * awayTeam.GoalsAgainstPerGame / 2.0;
        double awayGoals = awayTeam.GoalsForPerGame * homeTeam.GoalsAgainstPerGame / 2.0;

        // Calculate the probability that each team scores at least one goal
        var homeScoreProb = homeTeam.GoalProbability * awayTeam.ConcededProbability;
        var awayScoreProb = awayTeam.GoalProbability * homeTeam.ConcededProbability;

        // Calculate the probability that both teams score at least one goal
        double bothScoreProb = homeTeam.GoalProbability * awayTeam.GoalProbability / 2.0;

        // Calculate the probability that both teams do not score any goals
        double neitherScoreProb = homeTeam.NoGoalPerformance * awayTeam.NoGoalPerformance;

        // Return true if the probability of both teams scoring is greater than the probability of neither team scoring
        return bothScoreProb > neitherScoreProb;
    }
}
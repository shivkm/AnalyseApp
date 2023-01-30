using AnalyseApp.Extensions;
using AnalyseApp.Models;
using Microsoft.ML;

namespace AnalyseApp.Services;
/*
public class CalculationService
{
    private readonly IPoissonService _service;
    private readonly List<GameData> _gameData;
    private readonly List<GameData> _upcomingGames;
    private readonly IList<GameData> _currentSeason;
    private readonly IList<GameData> _lastSixSeason;

    private List<PoissonProbability> _poissonProbability;
    private (TeamPerformance Home, TeamPerformance Away) _lastEightMatches;
    private (TeamPerformance Home, TeamPerformance Away) _headToHeadMatches;
    private (TeamPerformance Home, TeamPerformance Away) _currentSeasonMatches;
    private (TeamPerformance Home, TeamPerformance Away) _lastSixSeasonMatches;
    private readonly List<Prediction> _NoGoalMatches = new ();
    private readonly List<Prediction> _moreThanTwoGoalMatches = new ();
    private readonly List<Prediction> _bothScoredGoalMatches = new ();
    private readonly List<Prediction> _lessGoalMatches = new ();
    private readonly List<Prediction> _WinMatches = new ();
    private readonly List<Prediction> _halftimeScores = new ();
    
    public CalculationService(IPoissonService service, List<GameData> gameData, List<GameData> upcomingGames)
    {
        _gameData = gameData;
        _service = service;
        _upcomingGames = upcomingGames;
        _currentSeason = gameData.GetGameDataBy(2022, 2023);
        _lastSixSeason = gameData.GetGameDataBy(2016, 2022);
    }


    public void Execute()
    {
        foreach (var upcomingGame in _upcomingGames)
        {
            AnalyseGamePerformance(upcomingGame.HomeTeam, upcomingGame.AwayTeam, upcomingGame.Div);
        }

        var gamesInOrder = ArrangeGamesInDescendingOrder();

        GenerateTickets(gamesInOrder);
    }

    private static void GenerateTickets(List<Prediction> gamesInOrder)
    {
        var count = 0;
        
        foreach (var game in gamesInOrder)
        {
            if (count == 0)
                Console.WriteLine("###### Generated Super Schein #######");
            
            if (count is 4 or 8 or 12)
                Console.WriteLine("###############################\n\n");
            
            
            if (count == 4)
                Console.WriteLine("######## Generated Second Schein ###########");
            
            
            if (count == 8)
                Console.WriteLine("######## Generated Third Schein ###########");

            if (count is 1 or 2 or 3)
            {
                Console.WriteLine(game.Msg);
            }
            {
                Console.WriteLine(game.Msg);
            }
            
            
            count++;
        }
    }

    private List<Prediction> ArrangeGamesInDescendingOrder()
    {
        var bothScoreMatches = _bothScoredGoalMatches.OrderByDescending(o => o.WkSmWeighting)
            .ToList();
        
        var noGoals = _NoGoalMatches.OrderByDescending(o => o.WkSmWeighting).ToList();
        var wins = _WinMatches.OrderByDescending(o => o.WkSmWeighting).ToList();

        var finalGames = new List<Prediction>();
        foreach (var bothScore in bothScoreMatches)
        {
            if (bothScore.WkSmWeighting is double.NaN)
                continue;

            var qualifiedAlsoForMoreThanTwoGoals = _moreThanTwoGoalMatches
                .OrderByDescending(o => o.WkSmWeighting)
                .Where(i => i.Key == bothScore.Key && i.WkSmWeighting > 65)
                .Take(15)
                .ToList();

            if (qualifiedAlsoForMoreThanTwoGoals.Any())
                finalGames.AddRange(qualifiedAlsoForMoreThanTwoGoals.Select(qualifiedAlsoForMoreThanTwoGoal => qualifiedAlsoForMoreThanTwoGoal with { Msg = $"{qualifiedAlsoForMoreThanTwoGoal.Key} {qualifiedAlsoForMoreThanTwoGoal.WkSmWeighting}% Qualified for over 2.5" }));
            
            var qualifiedAlsoForHalftime = _halftimeScores
                .OrderByDescending(o => o.WkSmWeighting)
                .Where(i => i.Key == bothScore.Key && i.WkSmWeighting > 65)
                .Take(15)
                .ToList();
            
            if (qualifiedAlsoForHalftime.Any())
                 finalGames.AddRange(qualifiedAlsoForHalftime.Select(halftime => halftime with { Msg = $"{halftime.Key} {halftime.WkSmWeighting}% Qualified for halftime score" }));
            
            var qualifiedAlsForLess = _lessGoalMatches
                .OrderByDescending(o => o.WkSmWeighting)
                .Take(15)
                .ToList();

            if (qualifiedAlsForLess.Any())
                finalGames.AddRange(qualifiedAlsForLess.Select(lessGoal => lessGoal with { Msg = $"{lessGoal.Key} {lessGoal.WkSmWeighting}% Qualified for less 2.5" }));
           
            var qualifiedAlsForNoGoal = _NoGoalMatches.OrderByDescending(o => o.WkSmWeighting)
                .Take(15)
                .ToList();

            if (qualifiedAlsForNoGoal.Any())
                finalGames.AddRange(qualifiedAlsForNoGoal.Select(noGoal => noGoal with { Msg = $"{noGoal.Key} {noGoal.WkSmWeighting}% Qualified for no goal" }));

            var qualifiedAlsBoth = finalGames.Where(i => i.Key != bothScore.Key)
                .Take(15)
                .ToList();

            if (qualifiedAlsBoth.Any())
                finalGames.Add(bothScore with{ Msg = $"{bothScore.Key} {bothScore.WkSmWeighting}% Qualified for both score" });
        }

        finalGames.AddRange(wins.Select(win => win with { Msg = $"{win.Key} is qualified for halftime: {Math.Round(win.WkSmWeighting, 2)}" }));

        return finalGames;
    }
    

    private void AnalyseGamePerformance(string homeTeam, string awayTeam, string league)
    {
        // Filter matches in current season for both team
        var currentMatches = _currentSeason
            .Where(i => i.Div == league && 
                i.HomeTeam == awayTeam || i.AwayTeam == homeTeam || 
                i.HomeTeam == homeTeam || i.AwayTeam == awayTeam)
            .ToList();

        _poissonProbability = _service.Execute(homeTeam, awayTeam, league);
        LastEightMatchesPerformance(currentMatches, homeTeam, awayTeam);
        CurrentSeasonPerformance(homeTeam, awayTeam);
        LastSixSeasonPerformance(homeTeam, awayTeam);
        HeadToHeadPerformance(homeTeam, awayTeam);

        var test = NoGoalMatches();
        EvaluateMoreThanTwoGoalsPerformance(homeTeam, awayTeam, league);
        EvaluateBothScoreGoalsPerformance(homeTeam, awayTeam, league);
        EvaluateLessGoalsPerformance(homeTeam, awayTeam, league);
        EvaluateWinPerformance(homeTeam, awayTeam, league);
        EvaluateHalftimeScoredGoalsPerformance(awayTeam, homeTeam, league);

    }

    private void HeadToHeadPerformance(string homeTeam, string awayTeam)
    {
        var headToHeads = _gameData
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam)
            .ToList();

        var homeTeamPerformance = CalculateTeamPerformanceBy(headToHeads, homeTeam);
        var awayTeamPerformance = CalculateTeamPerformanceBy(headToHeads, awayTeam);

        _headToHeadMatches = (homeTeamPerformance, awayTeamPerformance);
    }
    
    private void LastEightMatchesPerformance(IReadOnlyCollection<GameData> currentMatches, string homeTeam, string awayTeam)
    {
        var lastEightHomeMatches = currentMatches
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .Take(8)
            .ToList();

        var lastEightAwayMatches = currentMatches
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .Take(8)
            .ToList();
        
        var homeTeamPerformance = CalculateTeamPerformanceBy(lastEightHomeMatches, homeTeam);
        var awayTeamPerformance = CalculateTeamPerformanceBy(lastEightAwayMatches, awayTeam);
        
        _lastEightMatches = (homeTeamPerformance, awayTeamPerformance);
    }
    
    private void CurrentSeasonPerformance(string homeTeam, string awayTeam)
    {
        var homeCurrentSeasonMatches = _currentSeason
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();

        var awayCurrentSeasonMatches = _currentSeason
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();
        
        var homeTeamPerformance = CalculateTeamPerformanceBy(homeCurrentSeasonMatches, homeTeam);
        var awayTeamPerformance = CalculateTeamPerformanceBy(awayCurrentSeasonMatches, awayTeam);
        
        _currentSeasonMatches = (homeTeamPerformance, awayTeamPerformance);
    }

    private void LastSixSeasonPerformance(string homeTeam, string awayTeam)
    {
        var homeLastSixSeason = _lastSixSeason
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();

        var awayLastSixSeason = _lastSixSeason
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();
        
        var homeTeamPerformance = CalculateTeamPerformanceBy(homeLastSixSeason, homeTeam);
        var awayTeamPerformance = CalculateTeamPerformanceBy(awayLastSixSeason, awayTeam);
        
        _lastSixSeasonMatches = (homeTeamPerformance, awayTeamPerformance);
    }

    private static TeamPerformance CalculateTeamPerformanceBy(IList<GameData> currentMatches, string team)
    {
        var performance = new TeamPerformance
        {
            MatchesPlayed = currentMatches.Count,
            Win = currentMatches.GetWinMatchCountBy(team),
            Loss = currentMatches.GetLossMatchCountBy(team),
            Draw = currentMatches.Count(i => i.FTR == "D"),
            NoGoalMatches = currentMatches.GetNoGoalMatchCountBy(),
            Shots = currentMatches.GetShotsCountBy(team),
            ShotsOnGoal = currentMatches.GetShotOnGoalsCountBy(team),
            Offsides = currentMatches.GetOffSideCountBy(team),
            FoulsCommitted = currentMatches.GetFoulCommittedCountBy(team),
            GoalsScored = currentMatches.GetGoalScoredMatchSumBy(team),
            GoalsConceded = currentMatches.GetGoalConcededMatchSumBy(team),
            OneSideGoalMatches = currentMatches.GetOneSideGoalMatchCountBy(),
            WinOneSideGoalMatches = currentMatches.Count(i => i.HomeTeam == team && i is { FTHG: > 0, FTAG: 0 } ||
                                                         i.AwayTeam == team && i is { FTHG: 0, FTAG: > 0 }),
            BothScoreMatches = currentMatches.GetBothScoredMatchCountBy(),
            MoreThanTwoGoalsMatches = currentMatches.GetMoreThanTwoGoalScoredMatchCountBy(),
            TwoToThreeGoalMatches = currentMatches.GetTwoToThreeGoalScoredMatchCountBy(),
            HalftimeGoalsScored = currentMatches.GetHalftimeGoalScoredSumBy(team),
            HalftimeGoalsConceded = currentMatches.GetHalftimeGoalConcededSumBy(team),
            HalftimeGoalMatches = currentMatches.GetHalftimeGoalScoredMatchCountBy()
        };

        return performance;
    }
    
    private bool NoGoalMatches()
    {
        // WK/SA Weighting Head to Head: 30 
        var homeNoGoalMatches =
            _headToHeadMatches.Home.NoGoalMatches * 0.30 +
            _currentSeasonMatches.Home.NoGoalMatches * 0.15 + 
            _lastSixSeasonMatches.Home.NoGoalMatches * 0.15 +
            _lastEightMatches.Home.NoGoalMatches * 0.40;
        
        var awayNoGoalMatches =
            _headToHeadMatches.Away.NoGoalMatches * 0.30 +
            _currentSeasonMatches.Away.NoGoalMatches * 0.15 + 
            _lastSixSeasonMatches.Away.NoGoalMatches * 0.15 +
            _lastEightMatches.Away.NoGoalMatches * 0.40;

        if (homeNoGoalMatches + awayNoGoalMatches < 0.30) return false;

        if (homeNoGoalMatches + awayNoGoalMatches > 0.60) return true;

        return false;
        //_NoGoalMatches.Add(new Prediction($"{homeTeam}:{awayTeam}", league, 0.0, average, 0.0));
    }
    
    private void EvaluateMoreThanTwoGoalsPerformance(string homeTeam, string awayTeam, string league)
    {
        var probability = _poissonProbability.FirstOrDefault(i => i.Key == "MoreThanTwoGoals")?.Probability ?? 0;
        var homeMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Home.CompositeMoreThanTwoGoals * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Home.CompositeMoreThanTwoGoals * 0.05 + 
             _lastSixSeasonMatches.Home.CompositeMoreThanTwoGoals * 0.05) +
            // Last eight games 
            _lastEightMatches.Home.CompositeMoreThanTwoGoals * 0.40 +
            // Poison Probability
            probability * 0.25;
        
        var awayMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Away.CompositeMoreThanTwoGoals * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Away.CompositeMoreThanTwoGoals * 0.05 + 
             _lastSixSeasonMatches.Away.CompositeMoreThanTwoGoals * 0.05) +
            // Last eight games 
            _lastEightMatches.Away.CompositeMoreThanTwoGoals * 0.40 +
            // Poison Probability
            probability * 0.25;

        if (_lastEightMatches.Away.MoreThanTwoGoalMatchPlayed > 4 ||
            _lastEightMatches.Home.MoreThanTwoGoalMatchPlayed > 4 &&
            homeMatches < 0.60 || awayMatches < 0.60) 
            return;
        
        var matchAverage = homeMatches * 0.45 + awayMatches * 0.55;
        _moreThanTwoGoalMatches.Add(new Prediction($"{homeTeam}:{awayTeam}",  league, 0.0, matchAverage, 0.0));
    }
    
    private void EvaluateBothScoreGoalsPerformance(string homeTeam, string awayTeam, string league)
    {
        var probability = _poissonProbability.FirstOrDefault(i => i.Key == "BothTeamScore")?.Probability ?? 0;
        var homeMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Home.CompositeScoreGoals * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Home.CompositeScoreGoals * 0.05 + 
             _lastSixSeasonMatches.Home.CompositeScoreGoals * 0.05) +
            // Last eight games 
            _lastEightMatches.Home.CompositeScoreGoals * 0.40 +
            // Poison Probability
            probability * 0.25;
        
        var awayMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Away.CompositeScoreGoals * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Away.CompositeScoreGoals * 0.05 + 
             _lastSixSeasonMatches.Away.CompositeScoreGoals * 0.05) +
            // Last eight games 
            _lastEightMatches.Away.CompositeScoreGoals * 0.40 +
            // Poison Probability
            probability * 0.25;

        
        var (homeOneSideGoalMatches, awayOneSideGoalMatches) = GetOneSidematchAverage();
        if (_lastEightMatches.Away.BothScoredMatchPlayed > 4 &&
            _lastEightMatches.Home.BothScoredMatchPlayed > 4 && 
            homeMatches < 0.60 || awayMatches < 0.60 &&
            homeOneSideGoalMatches > 0.40 || awayOneSideGoalMatches > 0.40) 
            return;
        
        var matchAverage = homeMatches * 0.45 + awayMatches * 0.55;
        _bothScoredGoalMatches.Add(new Prediction($"{homeTeam}:{awayTeam}",  league, 0.0, matchAverage, 0.0));
    }
    
    private void EvaluateHalftimeScoredGoalsPerformance(string homeTeam, string awayTeam, string league)
    {
        var homeMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Home.CompositeHalftimeGoals * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Home.CompositeHalftimeGoals * 0.08 + 
             _lastSixSeasonMatches.Home.CompositeHalftimeGoals * 0.17) +
            // Last eight games 
            _lastEightMatches.Home.CompositeHalftimeGoals * 0.50;
        
        var awayMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Away.CompositeHalftimeGoals * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Away.CompositeHalftimeGoals * 0.08 + 
             _lastSixSeasonMatches.Away.CompositeHalftimeGoals * 0.17) +
            // Last eight games 
            _lastEightMatches.Away.CompositeHalftimeGoals * 0.50;

        
        var (homeOneSideGoalMatches, awayOneSideGoalMatches) = GetOneSidematchAverage();
        if (_lastEightMatches.Away.BothScoredMatchPlayed > 4 &&
            _lastEightMatches.Home.BothScoredMatchPlayed > 4 && 
            homeMatches < 0.60 || awayMatches < 0.60 &&
            homeOneSideGoalMatches > 0.40 || awayOneSideGoalMatches > 0.40) 
            return;
        
        var matchAverage = homeMatches * 0.45 + awayMatches * 0.55;
        _halftimeScores.Add(new Prediction($"{homeTeam}:{awayTeam}",  league, 0.0, matchAverage, 0.0));
    }

    private void EvaluateLessGoalsPerformance(string homeTeam, string awayTeam, string league)
    {
        var probability = _poissonProbability.FirstOrDefault(i => i.Key == "LessThanTwoGoals")?.Probability ?? 0;
        var homeMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Home.CompositeLessGoals * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Home.CompositeLessGoals * 0.05 + 
             _lastSixSeasonMatches.Home.CompositeLessGoals * 0.05) +
            // Last eight games 
            _lastEightMatches.Home.CompositeLessGoals * 0.40 +
            // Poison Probability
            probability * 0.25;
        
        var awayMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Away.CompositeLessGoals * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Away.CompositeLessGoals * 0.05 + 
             _lastSixSeasonMatches.Away.CompositeLessGoals * 0.05) +
            // Last eight games 
            _lastEightMatches.Away.CompositeLessGoals * 0.40 +
            // Poison Probability
            probability * 0.25;

        var (homeOneSideGoalMatches, awayOneSideGoalMatches) = GetOneSidematchAverage();

        if (_lastEightMatches.Away.MoreThanTwoGoalMatchPlayed > 4 &&
            _lastEightMatches.Home.MoreThanTwoGoalMatchPlayed > 4 && 
            homeMatches < 0.60 || awayMatches < 0.60 &&
            homeOneSideGoalMatches < 0.35 || awayOneSideGoalMatches < 0.35) 
            return;
        
        var matchAverage = homeMatches * 0.45 + awayMatches * 0.55;
        _lessGoalMatches.Add(new Prediction($"{homeTeam}:{awayTeam}",  league, 0.0, matchAverage, 0.0));
    }

     
    private void EvaluateWinPerformance(string homeTeam, string awayTeam, string league)
    {
        var (homeMatches, awayMatches) = GetOneSidematchAverage();

        if (homeMatches < 0.30 || awayMatches < 0.30) 
            return;
        
        var matchAverageHomeOneSideGoalAverage = homeMatches;
        var matchAverageAwayOneSideGoalAverage = awayMatches;
        
        homeMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Home.WinOneSideGoalMatches * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Home.WinOneSideGoalMatches * 0.10 + 
             _lastSixSeasonMatches.Home.WinOneSideGoalMatches * 0.15) +
            // Last eight games 
            _lastEightMatches.Home.WinOneSideGoalMatches * 0.50;
        
        awayMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Away.WinOneSideGoalMatches * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Away.WinOneSideGoalMatches * 0.10 + 
             _lastSixSeasonMatches.Away.WinOneSideGoalMatches * 0.15) +
            // Last eight games 
            _lastEightMatches.Away.WinOneSideGoalMatches * 0.50;
        
        var matchAverageWinHomeOneSideGoalAverage = homeMatches;
        var matchAverageWinAwayOneSideGoalAverage = awayMatches;

        var oneSideHomeWinAccuracy = matchAverageWinHomeOneSideGoalAverage / matchAverageHomeOneSideGoalAverage;
        var oneSideAwayWinAccuracy = matchAverageWinAwayOneSideGoalAverage / matchAverageAwayOneSideGoalAverage;
        
        var homeProbability = _poissonProbability.FirstOrDefault(i => i.Key == "HomeWin")?.Probability ?? 0;
        var awayProbability = _poissonProbability.FirstOrDefault(i => i.Key == "AwayWin")?.Probability ?? 0;
        homeMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Home.CompositeWin * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Home.CompositeWin * 0.05 + 
             _lastSixSeasonMatches.Home.CompositeWin * 0.05) +
            // Last eight games 
            _lastEightMatches.Home.CompositeWin * 0.40 +
            // Poison Probability
            homeProbability * 0.25;
        
        awayMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Away.CompositeWin * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Away.CompositeWin * 0.05 + 
             _lastSixSeasonMatches.Away.CompositeWin * 0.05) +
            // Last eight games 
            _lastEightMatches.Away.CompositeWin * 0.40 +
            // Poison Probability
            awayProbability * 0.25;
        
        if(homeMatches < 0.40 && awayMatches < 0.40)
            return;

        var home = oneSideHomeWinAccuracy * 0.4 + homeMatches * 0.6;
        var away = oneSideAwayWinAccuracy * 0.4 + awayMatches * 0.6;
        if (home > away)
        {
            _WinMatches.Add(new Prediction($"{homeTeam}",  league, 0.0, home, 0.0));
        }

        if (away > home)
        {
            _WinMatches.Add(new Prediction($"{awayTeam}",  league, 0.0, away, 0.0));
        }
    }

    private (double homeMatches, double awayMatches) GetOneSidematchAverage()
    {
        var probability = _poissonProbability.FirstOrDefault(i => i.Key == "OneSideGoal")?.Probability ?? 0;
        var homeMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Home.OneSideGoalMatches * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Home.OneSideGoalMatches * 0.05 +
             _lastSixSeasonMatches.Home.OneSideGoalMatches * 0.05) +
            // Last eight games 
            _lastEightMatches.Home.OneSideGoalMatches * 0.40 +
            // Poison Probability
            probability * 0.25;

        var awayMatches =
            // Head to Head WK/SA Weighting
            _headToHeadMatches.Away.OneSideGoalMatches * 0.25 +
            // Historical currentseason + last six season
            (_currentSeasonMatches.Away.OneSideGoalMatches * 0.05 +
             _lastSixSeasonMatches.Away.OneSideGoalMatches * 0.05) +
            // Last eight games 
            _lastEightMatches.Away.OneSideGoalMatches * 0.40 +
            // Poison Probability
            probability * 0.25;
        return (homeMatches, awayMatches);
    }


 
    

    public void Execute(string homeTeam, string awayTeam, string league)
    {
        AnalyseGamePerformance(homeTeam, awayTeam, league);
        var context = new MLContext();
       
        var trainingData = _gameData
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == awayTeam || 
                                i.HomeTeam == awayTeam || i.AwayTeam == homeTeam)
            .Select(s => new MatchData
            {
                HomeTeamGoals = Convert.ToSingle(s.FTHG),
                AwayTeamGoals = Convert.ToSingle(s.FTAG),
                HomeTeamHalfTimeGoals = Convert.ToSingle(s.HTHG),
                AwayTeamHalfTimeGoals = Convert.ToSingle(s.HTAG),
                HomeTeamFullTimeGoals = Convert.ToSingle(s.FTHG),
                AwayTeamFullTimeGoals = Convert.ToSingle(s.FTAG),
                AwayTeamShots = Convert.ToSingle(s.AS),
                HomeTeamShots = Convert.ToSingle(s.HS)
            });
        
        var trainingDataView = context.Data.LoadFromEnumerable(trainingData);

        var pipeline = context.Transforms.Concatenate("Features", "HomeTeamGoals", "AwayTeamGoals", "HomeTeamShots", "AwayTeamShots", "HomeTeamHalfTimeGoals", "AwayTeamHalfTimeGoals", "HomeTeamFullTimeGoals", "AwayTeamFullTimeGoals")
            .Append(context.Transforms.NormalizeMinMax("Features"))
            .Append(context.Transforms.Conversion.MapValueToKey("HomeTeamGoals"))
            .Append(context.Transforms.Conversion.MapValueToKey("AwayTeamGoals"))
            .Append(context.Transforms.Conversion.MapValueToKey("HomeTeamShots"))
            .Append(context.Transforms.Conversion.MapValueToKey("AwayTeamShots"))
            .Append(context.Transforms.Conversion.MapValueToKey("HomeTeamHalfTimeGoals"))
            .Append(context.Transforms.Conversion.MapValueToKey("AwayTeamHalfTimeGoals"))
            .Append(context.Transforms.Conversion.MapValueToKey("HomeTeamFullTimeGoals"))
            .Append(context.Transforms.Conversion.MapValueToKey("AwayTeamFullTimeGoals"))
            .Append(context.Regression.Trainers.Sdca("PredictedLabel"));

        var model = pipeline.Fit(trainingDataView);
        
        // Creating the prediction engine
        var predictionEngine = context.Model.CreatePredictionEngine<MatchData, MatchPrediction>(model);

       
        
        
        
        for (var homeScore = 0; homeScore <= 10; homeScore++)
        {
            for (var awayScore = 0; awayScore <= 10; awayScore++)
            {
                var matchSample = new MatchData { HomeTeamGoals = homeScore, AwayTeamGoals = awayScore,
                    HomeTeamShots = homeScore + 4, AwayTeamShots = awayScore + 4, 
                    HomeTeamHalfTimeGoals = homeScore > 0 ? homeScore - 1: homeScore, 
                    AwayTeamHalfTimeGoals = awayScore > 0 ? awayScore - 1 : awayScore,
                    HomeTeamFullTimeGoals = homeScore, AwayTeamFullTimeGoals = awayScore};
                
                // Getting the prediction
                var prediction = predictionEngine.Predict(matchSample);
                Console.WriteLine("Predicted goal average: " + prediction.PredictedLabel);


            }
        }
        
        
        
        // Convert IEnumerable collections to IDataView
        //var allGgames = _gameData.GetLeagueSeasonBy(2018, 2022, league);
        //var model = Train(homeTeam, awayTeam);
        //PredictMatch(model);

        /*  var result = new List<MatchProbability>();
        var lastSeasons = AnalyseTheSeasonPerformanceBy(homeTeam, awayTeam, league, 2018, 2022);
        var currentSeason = AnalyseTheSeasonPerformanceBy(homeTeam, awayTeam, league, 2022, 2023); 
        var currentGameBookmakers = GetBet365BookmakersValuesBy(homeTeam, awayTeam);

         var homeBet365 = 1 / 2.0;
         var awayBet365 = 1 / 3.60;
         var drawBet365 = 1 / 3.40;
         var goalGoalBet365 = 1 / 1.90;
         var moreThanTwoGoalBet365 = 1 / 2.10;
         var TwoToThreeGoalBet365 = 1 / 2.05;
         var lessThanThreeGoalBet365 = 1 / 1.72;

        
        var homeBet365 = 1 / 2.45;
        var awayBet365 = 1 / 2.90;
        var drawBet365 = 1 / 3.80;
        var goalGoalBet365 = 1 / 2.00;
        var moreThanTwoGoalBet365 = 1 / 2.37;
        var TwoToThreeGoalBet365 = 1 / 2.00;
        var lessThanThreeGoalBet365 = 1 / 1.57;
        
        var homeBet365 = 1 / 1.40;
        var awayBet365 = 1 / 7.50;
        var drawBet365 = 1 / 5.00;
        var goalGoalBet365 = 1 / 1.80;
        var moreThanTwoGoalBet365 = 1 / 1.61;
        var TwoToThreeGoalBet365 = 1 / 2.00;
        var lessThanThreeGoalBet365 = 1 / 2.30;
        var home = CalculateWeighting(lastSeasons["HomeWin"], currentSeason["HomeWin"]);
        var away = CalculateWeighting(lastSeasons["AwayWin"], currentSeason["AwayWin"]);
        var draw = CalculateWeighting(lastSeasons["Draw"], currentSeason["Draw"]);
        var goalGoal = CalculateWeighting(lastSeasons["BothTeamScore"], currentSeason["BothTeamScore"]);
        var moreThanTwoGoals = CalculateWeighting(lastSeasons["MoreThanTwoGoals"], currentSeason["MoreThanTwoGoals"]);
        var twoToThreeGoal = CalculateWeighting(lastSeasons["TwoToThree"], currentSeason["TwoToThree"]);
        var zeroZeroGoal = CalculateWeighting(lastSeasons["ZeroZeroGoal"], currentSeason["ZeroZeroGoal"]);
        var lessThanThreeGoal = CalculateWeighting(lastSeasons["LessThanTwoGoals"], currentSeason["LessThanTwoGoals"]);
        
        foreach (var allSeason in lastSeasons)
        {
            var currentSeasonProbability = currentSeason
                .Where(i => i.Key == allSeason.Key)
                .Select(ii => ii.Value)
                .FirstOrDefault();

           
              
            result.Add(new MatchProbability
            {
                Key = allSeason.Key,
                Probability = allSeason.Value.CalculateWeighting(currentSeasonProbability),
                Bet365BookMaker = 1 / GetBet365BookMakersProbabilityBy(currentGameBookmakers, allSeason.Key)
            });
            
        }

        return result;
        ;
        //  var homeWin = lastSeason.Values + currentSeason.Values;
        //  var awayWin = awayWinMatches.Sum(i => i.Value[1]);
        //  var draw = drawMatches.Sum(i => i.Value[0]);



    }
    



    
    private static (double WkSahilAverage, double RajevAverage, double ShivM) ZeroZeroGameAverage(
        Average? historicalMatches, 
        Average? currentSeason, 
        Average? lastSixMatches,
        HeadToHead? headToHead,
        IEnumerable<PoissonProbability> probabilities
        )
    {
        var zeroZeroProbability = probabilities.First(i => i.Key == "ZeroZeroGoals");
        if (zeroZeroProbability.Probability > 25)
            return (zeroZeroProbability.Probability, zeroZeroProbability.Probability, zeroZeroProbability.Probability);

        var historicalAverage = GetHistoricalAverage(historicalMatches?.ZeroZeroGame, currentSeason?.ZeroZeroGame);
        var lastSixGamesAverage = lastSixMatches?.ZeroZeroGame.Home + lastSixMatches?.ZeroZeroGame.Away ?? 0;

        var finalAverage = CalculateWeightingBy(
            lastSixGamesAverage, 
            historicalAverage,
            headToHead?.NoScore, 
            zeroZeroProbability
        );
        
        var finalAverage1 = CalculateWeightingBy(
            lastSixGamesAverage, 
            historicalAverage,
            headToHead?.NoScore, 
            zeroZeroProbability,
            1
        );
        
        var finalAverage2 = CalculateWeightingBy(
            lastSixGamesAverage, 
            historicalAverage,
            headToHead?.NoScore, 
            zeroZeroProbability,
            2
        );

        return (Math.Round(finalAverage ?? 0, 2), Math.Round(finalAverage1 ?? 0, 2), Math.Round(finalAverage2 ?? 0, 2));
    }
    
    private static (double WkSahilAverage, double RajevAverage, double ShivM) OneSideGameAverage(
        Average? historicalMatches, 
        Average? currentSeason, 
        Average? lastSixMatches,
        HeadToHead? headToHead,
        IEnumerable<PoissonProbability> probabilities
    )
    {
        var oneSideGoal = probabilities.First(i => i.Key == "OneSideGoal");
        if (oneSideGoal.Probability > 25)
            return (oneSideGoal.Probability, oneSideGoal.Probability, oneSideGoal.Probability);

        var historicalAverage = GetHistoricalAverage(historicalMatches?.OneSideResult, currentSeason?.OneSideResult);
        var lastSixGamesAverage = lastSixMatches?.OneSideResult.Home + lastSixMatches?.OneSideResult.Away ?? 0;

        var finalAverage = CalculateWeightingBy(
            lastSixGamesAverage, 
            historicalAverage,
            headToHead?.AwaySideResult + headToHead?.HomeSideResult, 
            oneSideGoal
        );
        
        var finalAverage1 = CalculateWeightingBy(
            lastSixGamesAverage, 
            historicalAverage,
            headToHead?.AwaySideResult + headToHead?.HomeSideResult, 
            oneSideGoal,
            1
        );
        
        var finalAverage2 = CalculateWeightingBy(
            lastSixGamesAverage, 
            historicalAverage,
            headToHead?.AwaySideResult + headToHead?.HomeSideResult, 
            oneSideGoal,
            2
        );

        return (Math.Round(finalAverage ?? 0, 2), Math.Round(finalAverage1 ?? 0, 2), Math.Round(finalAverage2 ?? 0, 2));
    }

    private static double GetHistoricalAverage(Result? historicalMatches, Result? currentSeason)
    {
        var historicalAverage = (historicalMatches?.Home + historicalMatches?.Away) * 0.40 +
            (currentSeason?.Home + currentSeason?.Away) * 0.60 ?? 0;
        
        return historicalAverage;
    }

    private static double? CalculateWeightingBy(
        double lastSixMatches, 
        double historical,
        double? headToHead, 
        PoissonProbability probability,
        int typeOf = 0)
    {
        var average = typeOf switch
        {
            0 => lastSixMatches * 0.40 + historical * 0.10 +
                 headToHead * 0.25 + probability.Probability * 0.25,
            
            1 => lastSixMatches * 0.40 + historical * 0.20 +
                 headToHead * 0.25 + probability.Probability * 0.15,
            
            2 => lastSixMatches * 0.30 + historical * 0.10 +
                 headToHead * 0.20 + probability.Probability * 0.40,
            _ => null
        };

        return average;
    }

    private Average? AnalyseCurrentSeasonBy(string homeTeam, string awayTeam, string league)
    {
        var currentSeason = _gameData.GetLeagueSeasonBy(2022, 2023, league);
        if (!currentSeason.TeamsAreInLeague(homeTeam, awayTeam))
            return null;

        var twoScoredGameHome = TeamScoredTwoGoalGames(currentSeason, homeTeam, true);
        var twoScoredGameAway = TeamScoredTwoGoalGames(currentSeason, awayTeam);
        var average = new Average
        {
            ZeroZeroGame =
            {
                Home = TeamZeroZeroGames(currentSeason, homeTeam, atHome: true),
                Away = TeamZeroZeroGames(currentSeason, awayTeam)
            },
            OneSideResult = 
            {
                Home = OneSideLessThanThreeScoredGamesAverage(currentSeason, awayTeam, true),
                Away = OneSideLessThanThreeScoredGamesAverage(currentSeason, awayTeam)
            },
            ScoredGames =
            {
                Home = TeamScoredGameAverage(currentSeason, homeTeam, atHome: true),
                Away = TeamScoredGameAverage(currentSeason, awayTeam)
            },
            HalftimeScoredGames = 
            {
                Home = TeamScoredGameAverage(currentSeason, homeTeam, halftime: true, atHome: true),
                Away = TeamScoredGameAverage(currentSeason, awayTeam, halftime: true)
            },
            TwoScoredGame =
            {
                Home = twoScoredGameHome.average,
                Away = twoScoredGameAway.average,
            },
            TwoScoredGameCount =
            {
                Home = twoScoredGameHome.count,
                Away = twoScoredGameAway.count
            },
            MoreThanTwoScoredAverage =
            {
                Home = MoreThanTwoGoalScoredGameAverage(currentSeason, homeTeam, atHome: true),
                Away = MoreThanTwoGoalScoredGameAverage(currentSeason, awayTeam)
            },
            BothScoredAverage =
            {
                Home = BothScoredGameAverage(currentSeason, homeTeam, atHome: true),
                Away = BothScoredGameAverage(currentSeason, awayTeam)
            }
        };

        return average;
    }
   
    private Average? AnalyseAllSeasonBy(string homeTeam, string awayTeam, string league)
    {
        var lastSixSeason = _gameData.GetLeagueSeasonBy(2017, 2022, league);
        if (!lastSixSeason.TeamsAreInLeague(homeTeam, awayTeam))
            return null;

        var twoScoredGameHome = TeamScoredTwoGoalGames(lastSixSeason, homeTeam, true);
        var twoScoredGameAway = TeamScoredTwoGoalGames(lastSixSeason, awayTeam);
        var average = new Average
        {
            ZeroZeroGame =
            {
                Home = TeamZeroZeroGames(lastSixSeason, homeTeam, atHome: true),
                Away = TeamZeroZeroGames(lastSixSeason, awayTeam)
            },
            OneSideResult = 
            {
                Home = OneSideLessThanThreeScoredGamesAverage(lastSixSeason, awayTeam, true),
                Away = OneSideLessThanThreeScoredGamesAverage(lastSixSeason, awayTeam)
            },
            ScoredGames =
            {
                Home = TeamScoredGameAverage(lastSixSeason, homeTeam, atHome: true),
                Away = TeamScoredGameAverage(lastSixSeason, awayTeam)
            },
            HalftimeScoredGames = 
            {
                Home = TeamScoredGameAverage(lastSixSeason, homeTeam, halftime: true, atHome: true),
                Away = TeamScoredGameAverage(lastSixSeason, awayTeam, halftime: true)
            },
            TwoScoredGame =
            {
                Home = twoScoredGameHome.average,
                Away = twoScoredGameAway.average,
            },
            TwoScoredGameCount =
            {
                Home = twoScoredGameHome.count,
                Away = twoScoredGameAway.count
            },
            MoreThanTwoScoredAverage =
            {
                Home = MoreThanTwoGoalScoredGameAverage(lastSixSeason, homeTeam, atHome: true),
                Away = MoreThanTwoGoalScoredGameAverage(lastSixSeason, awayTeam)
            },
            BothScoredAverage =
            {
                Home = BothScoredGameAverage(lastSixSeason, homeTeam, atHome: true),
                Away = BothScoredGameAverage(lastSixSeason, awayTeam)
            },
            HeadToHeadGame = TeamHeadToHeadGameAverage(lastSixSeason, homeTeam, awayTeam)
        };

        return average;
    }

    private Average? AnalyseLastSixGamesBy(string homeTeam, string awayTeam, string league)
    {
        var lastSixGames = _gameData.GetLeagueSeasonBy(2022, 2023, league)
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam ||
                                i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .Take(6)
            .ToList();
        if (!lastSixGames.TeamsAreInLeague(homeTeam, awayTeam))
            return null;

        var twoScoredGameHome = TeamScoredTwoGoalGames(lastSixGames, homeTeam, true);
        var twoScoredGameAway = TeamScoredTwoGoalGames(lastSixGames, awayTeam);
        var average = new Average
        {
            ZeroZeroGame =
            {
                Home = TeamZeroZeroGames(lastSixGames, homeTeam, atHome: true),
                Away = TeamZeroZeroGames(lastSixGames, awayTeam)
            },
            OneSideResult = 
            {
                Home = OneSideLessThanThreeScoredGamesAverage(lastSixGames, awayTeam, true),
                Away = OneSideLessThanThreeScoredGamesAverage(lastSixGames, awayTeam)
            },
            ScoredGames =
            {
                Home = TeamScoredGameAverage(lastSixGames, homeTeam, atHome: true),
                Away = TeamScoredGameAverage(lastSixGames, awayTeam)
            },
            HalftimeScoredGames = 
            {
                Home = TeamScoredGameAverage(lastSixGames, homeTeam, halftime: true, atHome: true),
                Away = TeamScoredGameAverage(lastSixGames, awayTeam, halftime: true)
            },
            TwoScoredGame =
            {
                Home = twoScoredGameHome.average,
                Away = twoScoredGameAway.average,
            },
            TwoScoredGameCount =
            {
                Home = twoScoredGameHome.count,
                Away = twoScoredGameAway.count
            },
            MoreThanTwoScoredAverage =
            {
                Home = MoreThanTwoGoalScoredGameAverage(lastSixGames, homeTeam, atHome: true),
                Away = MoreThanTwoGoalScoredGameAverage(lastSixGames, awayTeam)
            },
            BothScoredAverage =
            {
                Home = BothScoredGameAverage(lastSixGames, homeTeam, atHome: true),
                Away = BothScoredGameAverage(lastSixGames, awayTeam)
            }
        };

        return average;
    }

    
    private static double MoreThanTwoGoalScoredGameAverage(IList<GameData> leagueSeason, string team, bool halftime = false, bool atHome = false)
    {
        var homeTeamHomeField = leagueSeason
            .Where(i => i.HomeTeam == team)
            .ToList();
        
        var homeTeamAwayField = leagueSeason
            .Where(i => i.AwayTeam == team)
            .ToList();

        var homeScoreGameAverage = homeTeamHomeField.GetPercent(i => halftime ? i.HTHG + i.HTAG  > 2 : i.FTHG + i.FTAG> 2);
        var awayScoreGameAverage = homeTeamAwayField.GetPercent(i => halftime ? i.HTAG  + i.HTHG > 2 : i.FTAG + i.FTAG > 2);
        var scoreAverage = homeScoreGameAverage.CalculateWeighting(awayScoreGameAverage, atHome ? 0.60 : 0.40);
        
        return  Math.Round(scoreAverage, 2);
    }
    
    private static double BothScoredGameAverage(IList<GameData> leagueSeason, string team, bool halftime = false, bool atHome = false)
    {
        var homeTeamHomeField = leagueSeason
            .Where(i => i.HomeTeam == team)
            .ToList();
        
        var homeTeamAwayField = leagueSeason
            .Where(i => i.AwayTeam == team)
            .ToList();

        var homeScoreGameAverage = homeTeamHomeField.GetPercent(i => halftime ? i is { HTHG: >= 1, HTAG: >= 1 } : i is { FTHG: >= 1, FTAG: >= 1 });
        var awayScoreGameAverage = homeTeamAwayField.GetPercent(i => halftime ? i is { HTHG: >= 1, HTAG: >= 1 } : i is { FTHG: >= 1, FTAG: >= 1 });
        var scoreAverage = homeScoreGameAverage.CalculateWeighting(awayScoreGameAverage, atHome ? 0.60 : 0.40);
        
        return  Math.Round(scoreAverage, 2);
    }
    

    private static double TeamScoredGameAverage(IList<GameData> leagueSeason, string team, bool halftime = false, bool atHome = false)
    {
        var homeTeamHomeField = leagueSeason
            .Where(i => i.HomeTeam == team)
            .ToList();
        
        var homeTeamAwayField = leagueSeason
            .Where(i => i.AwayTeam == team)
            .ToList();

        var homeScoreGameAverage = homeTeamHomeField.GetPercent(i => halftime ? i.HTHG  > 0 : i.FTHG > 0);
        var awayScoreGameAverage = homeTeamAwayField.GetPercent(i => halftime ? i.HTAG  > 0 : i.FTAG > 0);
        var scoreAverage = homeScoreGameAverage.CalculateWeighting(awayScoreGameAverage, atHome ? 0.60 : 0.40);
        
        return  Math.Round(scoreAverage, 2);
    }

    private static double TeamZeroZeroGames(IList<GameData> leagueSeason, string team, bool atHome = false)
    {
        var homeTeamHomeField = leagueSeason.Where(i => i.HomeTeam == team).ToList();
        var homeTeamAwayField = leagueSeason.Where(i => i.AwayTeam == team).ToList();

        var homeScoreGamesAverage = homeTeamHomeField.GetPercent(i => i.FTHG == 0);
        var awayScoreGamesAverage = homeTeamAwayField.GetPercent(i => i.FTAG == 0);
        
        var scoreAverage = homeScoreGamesAverage
            .CalculateWeighting(awayScoreGamesAverage, atHome ? 0.60 : 0.40);

        return Math.Round(scoreAverage, 2);
    }
    
    private static double OneSideLessThanThreeScoredGamesAverage(IList<GameData> leagueSeason, string team, bool atHome = false)
    {
        var homeTeamHomeField = leagueSeason.Where(i => i.HomeTeam == team).ToList();
        var homeTeamAwayField = leagueSeason.Where(i => i.AwayTeam == team).ToList();

        var homeScoreGameAverage = homeTeamHomeField.GetPercent(i => atHome 
            ? i is { FTHG: >= 1 and < 3, FTAG: 0 }
            : i is { FTAG: >= 1 and < 3, FTHG: 0 });
        var awayScoreGameAverage = homeTeamAwayField.GetPercent(i => atHome 
            ? i is { FTAG: >= 1 and < 3, FTHG: 0 }
            : i is { FTHG: >= 1 and < 3, FTAG: 0 });
        
        var scoreAverage = homeScoreGameAverage
            .CalculateWeighting(awayScoreGameAverage, atHome ? 0.60 : 0.40);

        return Math.Round(scoreAverage, 3);
    }
  
    private static (double average, int count) TeamScoredTwoGoalGames(IList<GameData> leagueSeason, string team, bool atHome = false)
    {
        var homeTeamHomeField = leagueSeason.Where(i => i.HomeTeam == team).ToList();
        var homeTeamAwayField = leagueSeason.Where(i => i.AwayTeam == team).ToList();

        var homeScoreGameAverage = homeTeamHomeField.GetPercent(i => i.FTHG + i.FTAG > 2);
        var awayScoreGameAverage = homeTeamAwayField.GetPercent(i => i.FTHG + i.FTAG > 2);
        var moreThanTwoScoredGameCount = homeTeamHomeField.Count(i => i.FTHG + i.FTAG > 2) +
                                    homeTeamAwayField.Count(i => i.FTHG + i.FTAG > 2);
        
        var scoreAverage = homeScoreGameAverage.CalculateWeighting(awayScoreGameAverage, atHome ? 0.60 : 0.40);

        return (Math.Round(scoreAverage, 2), moreThanTwoScoredGameCount);
    }
 
    private static HeadToHead TeamHeadToHeadGameAverage(IEnumerable<GameData> leagueSeason, string homeTeam, string awayTeam)
    {
        var teamHomeField = leagueSeason
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                                i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .ToList();

        if (teamHomeField.Count == 0)
            return new HeadToHead();
        
        var headToHead = new HeadToHead
        {
            TotalGames = teamHomeField.Count,
            BothTeamScore = teamHomeField.GetPercent(i => i is { FTHG: >= 1, FTAG: >= 1 }) * 100,
            MoreThanTwoScore = teamHomeField.GetPercent(i => i.FTHG + i.FTAG > 2)* 100,
            TwoToThreeScore = teamHomeField.GetPercent(i => i.FTHG + i.FTAG == 2 || i.FTHG + i.FTAG == 3)* 100,
            NoScore = teamHomeField.GetPercent(i => i is { FTHG: 0, FTAG: 0 }) * 100,
            HalfTimeScore = teamHomeField.GetPercent(i => i.FTHG > 0 || i.FTAG > 0)* 100,
            HomeSideResult = teamHomeField.GetPercent(i => i is { FTHG: 1 or 2, FTAG: 0 })* 100,
            AwaySideResult = teamHomeField.GetPercent(i => i is { FTHG: 0, FTAG: 1 or 2 })* 100
        };

        return headToHead;
    }
    
}*/
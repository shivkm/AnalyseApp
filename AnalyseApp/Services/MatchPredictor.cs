using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class MatchPredictor: IMatchPredictor
{   
    private readonly List<Matches> _historicalMatches;
    private readonly IPoissonService _poissonService;
    private readonly IDataService _dataService;
    private readonly IFileProcessor _fileProcessor;

    private PoissonProbability _current = default!;
    private HeadToHeadData _headToHeadData = default!;
    private TeamData _homeTeamData = default!;
    private TeamData _awayTeamData = default!;
    private const string OverTwoGoals = "Over Two Goals";
    private const string UnderThreeGoals = "Under Three Goals";
    private const string BothTeamScore = "Both Team Score Goals";
    private const string TwoToThreeGoals = "Two to three Goals";
    private const string HomeWin = "Home will win";
    private const string AwayWin = "Away will win";
    
    
    private const double Sixty = 0.60;
    private const double Fifty = 0.50;
    
    public MatchPredictor(IFileProcessor fileProcessor, IPoissonService poissonService, IDataService dataService)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy().AssignSeasonsToMatches();
        _poissonService = poissonService;
        _dataService = dataService;
        _fileProcessor = fileProcessor;
    }

    public void Execute()
    {
        var upcoming = _fileProcessor.GetUpcomingGames();
       // SelfCheck();
        var count = 0;

        foreach (var game in upcoming)
        {
            var prediction = Execute(game.HomeTeam, game.AwayTeam, game.Date);

            if (prediction.Qualified)
            {
                count++;
                Console.WriteLine($"{game.Date} - {game.HomeTeam}:{game.AwayTeam} {prediction.Msg}");
            }
        }
        Console.WriteLine($"Count: {count}");
    }

    public Prediction Execute(string home, string away, string playedOn, BetType? betType = BetType.Unknown)
    {
        InitializeData(home, away, playedOn);

        var specificBetType = betType is not BetType.Unknown;
        var bothTeamScoreGoal = CurrentBothTeamScoreGoalChances();
        var overTwoGoals = CurrentOverTwoScoreGoalChances();
        var twoToThreeGoals = CurrentTwoToThreeGoalChances();
        var underThreeGoals = CurrentUnderThreeGoalChances();
        var winPredictions = CurrentWinChances();

        if ((!specificBetType || specificBetType && betType == BetType.OverTwoGoals) &&
            overTwoGoals.Qualified && (!overTwoGoals.HeadToHeadIgnored || overTwoGoals.HeadToHeadIgnored &&
                !bothTeamScoreGoal.Qualified && !twoToThreeGoals.HeadToHeadIgnored))
        {
            return new Prediction(overTwoGoals.Percentage, BetType.OverTwoGoals)   
            { 
                Qualified = true, 
                Msg = OverTwoGoals + $" {overTwoGoals.Percentage:F}% \n{overTwoGoals.Msg}"
            };
        }
        
        if ((!specificBetType || specificBetType && betType == BetType.BothTeamScoreGoals) && bothTeamScoreGoal.Qualified 
            && (!bothTeamScoreGoal.HeadToHeadIgnored || bothTeamScoreGoal.HeadToHeadIgnored && !twoToThreeGoals.HeadToHeadIgnored))
        {
            return new Prediction(bothTeamScoreGoal.Percentage, BetType.BothTeamScoreGoals)   
            { 
                Qualified = true, 
                Msg = BothTeamScore + $" {bothTeamScoreGoal.Percentage:F}% \n{bothTeamScoreGoal.Msg}"
            };
        }        
        
        if ((!specificBetType || specificBetType && betType == BetType.TwoToThreeGoals) && twoToThreeGoals.Qualified)
        {
            return new Prediction(twoToThreeGoals.Percentage, BetType.TwoToThreeGoals)   
            { 
                Qualified = true, 
                Msg = TwoToThreeGoals + $" {twoToThreeGoals.Percentage:F}% \n{twoToThreeGoals.Msg}"
            };
        }

        if ((!specificBetType || specificBetType && betType == BetType.HomeWin) && winPredictions is { Qualified: true, IsHome: true })
        {
            return new Prediction(winPredictions.Percentage, BetType.HomeWin)   
            { 
                Qualified = true, 
                Msg = HomeWin + $" {winPredictions.Percentage:F}% \n{winPredictions.Msg}"
            };
        }
        
        if ((!specificBetType || specificBetType && betType == BetType.AwayWin) && winPredictions is { Qualified: true, IsHome: false })
        {
            return new Prediction(winPredictions.AwayPercentage, BetType.AwayWin)   
            { 
                Qualified = true, 
                Msg = AwayWin + $" {winPredictions.AwayPercentage:F}% \n{winPredictions.Msg}"
            };
        }
        //
        // if (twoToThreeGoals is { Qualified: true } &&
        //     betType != BetType.BothTeamScoreGoals &&
        //     (!underThreeGoals.Qualified || underThreeGoals.Qualified && twoToThreeGoals.Percentage >= underThreeGoals.Percentage) 
        //     && !homeWinPossible && !awayWinPossible)
        // {
        //     return new Prediction(twoToThreeGoals.Percentage, BetType.TwoToThreeGoals)
        //     { 
        //         Qualified = true, 
        //         Msg = TwoToThreeGoals + $" {twoToThreeGoals.Percentage:F}%"
        //     };
        // }
        //         
        // if (underThreeGoals is { Qualified: true, Percentage: >= 0.55 } &&
        //     !winPredictions.Qualified &&
        //     betType != BetType.BothTeamScoreGoals &&
        //     _homeTeamData.LastThreeMatchResult != BetType.UnderThreeGoals &&
        //     _awayTeamData.LastThreeMatchResult != BetType.UnderThreeGoals && !homeWinPossible && !awayWinPossible)
        // {
        //     return new Prediction(underThreeGoals.Percentage, BetType.UnderThreeGoals)
        //     { 
        //         Qualified = true, 
        //         Msg = UnderThreeGoals + $" {underThreeGoals.Percentage:F}%"
        //     };;
        // }
        //
        // if (
        //     betType != BetType.BothTeamScoreGoals && winPredictions is { Qualified: true, isHome: true, HomePercentage: >= 0.50 } ||
        //     (winPredictions.HomePercentage >= 1.0 && winPredictions.HomePercentage > winPredictions.AwayPercentage))
        // {
        //     return new Prediction(winPredictions.HomePercentage, BetType.HomeWin)
        //     {
        //         Qualified = true,
        //         Msg = HomeWin + $" {winPredictions.HomePercentage:F}%"
        //     };
        // }
        //
        // if (
        //     betType != BetType.BothTeamScoreGoals && winPredictions is { Qualified: true, isHome: false, AwayPercentage: >= 0.50 } ||
        //     (winPredictions.AwayPercentage >= 1.0 && winPredictions.AwayPercentage > winPredictions.HomePercentage))
        // {
        //     return new Prediction(winPredictions.AwayPercentage, BetType.AwayWin)
        //     {
        //         Qualified = true,
        //         Msg = AwayWin + $" {winPredictions.HomePercentage:F}%"
        //     };
        // }
        //
        // // if the code reach this area that means all previous prediction failed to approve
        // if (
        //     betType != BetType.BothTeamScoreGoals &&
        //     _homeTeamData.HomeScoringPower > 0.30 && _awayTeamData.AwayScoringPower > 0.30 &&
        //     (overTwoGoals.Percentage > bothTeamScoreGoal.Percentage || overTwoGoals.Percentage <= bothTeamScoreGoal.Percentage && !string.IsNullOrEmpty(bothTeamScoreGoal.Msg)) &&
        //     overTwoGoals.Percentage > underThreeGoals.Percentage &&
        //     _homeTeamData.UnderTwoScoredGames <= 0.50 && _awayTeamData.UnderTwoScoredGames <= 0.50 && _homeTeamData.TwoToThreeGoalsGames <= 0.50 && _awayTeamData.TwoToThreeGoalsGames <= 0.50 &&
        //     (overTwoGoals.Percentage > twoToThreeGoals.Percentage ||
        //      _homeTeamData.OverScoredGames >= 0.50 && _awayTeamData.OverScoredGames >= 0.50 && _headToHeadData.OverScoredGames >= 0.50 && _homeTeamData.ScoringPower >= 0.62 && _awayTeamData.ScoringPower >= 0.62))
        // {
        //     return new Prediction(overTwoGoals.Percentage, BetType.OverTwoGoals)
        //     {
        //         Qualified = true,
        //         Msg = OverTwoGoals + $" risky: {overTwoGoals.Percentage:F}%"
        //     };
        // }
        //
        // if (
        //     betType != BetType.BothTeamScoreGoals && bothTeamScoreGoal.Qualified && bothTeamScoreGoal.Percentage > twoToThreeGoals.Percentage && 
        //     _homeTeamData.BothTeamScoredGames >= 0.50 && _awayTeamData.BothTeamScoredGames >= 0.50)
        // {
        //     return new Prediction(bothTeamScoreGoal.Percentage, BetType.BothTeamScoreGoals)    
        //     {
        //         Qualified = true,
        //         Msg = BothTeamScore + $" risky {bothTeamScoreGoal.Percentage:F}%"
        //     };
        // }    
        //
        // if (
        //     betType != BetType.BothTeamScoreGoals && (twoToThreeGoals is { Qualified: true, Percentage: > 0.55 } || twoToThreeGoals.Percentage >= underThreeGoals.Percentage) &&
        //     _homeTeamData.HomeScoringPower >= 0.20 && _awayTeamData.AwayScoringPower >= 0.20)
        // {
        //     return new Prediction(twoToThreeGoals.Percentage, BetType.TwoToThreeGoals)
        //     {
        //         Qualified = true,
        //         Msg = TwoToThreeGoals + $" risky {twoToThreeGoals.Percentage:F}%"
        //     };
        // }
        //
        // if ( 
        //     betType != BetType.BothTeamScoreGoals && _homeTeamData.ScoringPower > _awayTeamData.ScoringPower &&
        //     _homeTeamData.HomeScoringPower > _awayTeamData.AwayScoringPower &&
        //     _homeTeamData.WinAvg > _awayTeamData.WinAvg &&
        //     (_headToHeadData.Count >= 2 && _headToHeadData.HomeTeamWon > _headToHeadData.AwayTeamWon))
        // {
        //     return new Prediction(winPredictions.HomePercentage, BetType.HomeWin)
        //     {
        //         Qualified = true,
        //         Msg = HomeWin + $" risky {winPredictions.HomePercentage:F}%"
        //     };
        // }
        //
        // if (
        //     betType != BetType.BothTeamScoreGoals && _awayTeamData.ScoringPower > _homeTeamData.ScoringPower &&
        //     _awayTeamData.HomeScoringPower > _homeTeamData.AwayScoringPower &&
        //     _awayTeamData.WinAvg > _homeTeamData.WinAvg &&
        //     (_headToHeadData.Count >= 2 && _headToHeadData.AwayTeamWon > _headToHeadData.HomeTeamWon))
        // {
        //     return new Prediction(winPredictions.AwayPercentage, BetType.AwayWin)
        //     {
        //         Qualified = true,
        //         Msg = AwayWin + $" risky {winPredictions.AwayPercentage:F}%"
        //     };
        // }

        return new Prediction(underThreeGoals.Percentage, BetType.UnderThreeGoals)
        {
            Qualified = true,
            Msg = UnderThreeGoals + $" risky {underThreeGoals.Percentage:F}%"
        };
    }

    private void InitializeData(string home, string away, string playedOn)
    {
        var playedOnDateTime = Convert.ToDateTime(playedOn);
        var historicalData = _historicalMatches.OrderMatchesBy(playedOnDateTime).ToList();
        
        _headToHeadData = _dataService.GetHeadToHeadDataBy(home, away, playedOn);
        _homeTeamData = _dataService.GetTeamDataBy(home, historicalData);
        _awayTeamData = _dataService.GetTeamDataBy(away, historicalData);
        _current = CalculatePoissonProbability(home, away, playedOn, true);
    }
    
    private Prediction CurrentBothTeamScoreGoalChances()
    { 
        var msg = "";
        var scoringPower = _homeTeamData.GetScoreProbability(_awayTeamData);
        var prediction = new Prediction(scoringPower.Total, BetType.BothTeamScoreGoals);
        
        if (_homeTeamData.LastThreeMatchResult == BetType.BothTeamScoreGoals)
            msg ="Risky: home last three games both team scores";
        
        if (_awayTeamData.LastThreeMatchResult == BetType.BothTeamScoreGoals)
            msg ="Risky: away last three games both team scores";

        if (_homeTeamData.HomeScoringPower <= 0.28 && _awayTeamData.AwayScoringPower <= 0.28)
            return prediction with { Qualified = false, Msg = "scoring power is too low!" };

        var homeQualification = _homeTeamData is
        {
            BothTeamScoredGames: >= Fifty,
            TeamScoredGames: >= Fifty,
            TeamAllowedGoalGames: >= Fifty
        };

        var awayQualification = _awayTeamData is
        {
            BothTeamScoredGames: >= Fifty,
            TeamScoredGames: >= Fifty,
            TeamAllowedGoalGames: >= Fifty
        };

        var headToHeadQualification = _headToHeadData.Count <= 2 || _headToHeadData.BothTeamScoredGames >= Fifty;

        return scoringPower switch
        {
            { Home: > Fifty, Away: > Fifty } when homeQualification && awayQualification && headToHeadQualification =>
               prediction with { Qualified = true, Msg = $"{msg}" },
            
            { Home: > Fifty, Away: > Fifty } when (homeQualification || awayQualification) && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg}" },
            
            { Home: > 0.30, Away: > Fifty } when homeQualification && awayQualification && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} home has low chances" },
            
            { Home: > Fifty, Away: > 0.30 } when homeQualification && awayQualification && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} away has low chances" },
            
            { Home: > Fifty, Away: > Fifty } when homeQualification && awayQualification => 
                prediction with { Qualified = true, Msg = $"{msg} head to head ignored", HeadToHeadIgnored = true },
            
            { Home: > 0.30, Away: > Fifty } when homeQualification && awayQualification =>
                prediction with { Qualified = true, Msg = $"{msg} head to head ignored", HeadToHeadIgnored = true },
            
            { Home: > Fifty, Away: > 0.30 } when homeQualification && awayQualification => 
                prediction with { Qualified = true, Msg = $"{msg} head to head ignored", HeadToHeadIgnored = true },
            
            { Total: > 0.50 } when homeQualification && awayQualification => 
                prediction with { Qualified = true, Msg = $"{msg} head to head ignored", HeadToHeadIgnored = true },
            
            _ => prediction
        };
    }

    private Prediction CurrentOverTwoScoreGoalChances()
    {
        var msg = string.Empty;
        var scoringPower = _homeTeamData.GetScoreProbability(_awayTeamData);
        var prediction = new Prediction(scoringPower.Total, BetType.OverTwoGoals);

        if (_homeTeamData.LastThreeMatchResult == BetType.OverTwoGoals)
            msg = "Risky: home last three games over two goal game";

        if (_awayTeamData.LastThreeMatchResult == BetType.OverTwoGoals)
            msg = "Risky: away last three games over two goal game";

        if (_homeTeamData.HomeScoringPower <= 0.28 && _awayTeamData.AwayScoringPower <= 0.28)
            return prediction with { Qualified = false };

        var homeQualification = _homeTeamData is
        {
            OverScoredGames: >= Fifty,
            TeamScoredGames: >= Fifty,
            TeamAllowedGoalGames: >= Fifty
        };

        var awayQualification = _awayTeamData is
        {
            OverScoredGames: >= Fifty,
            TeamScoredGames: >= Fifty,
            TeamAllowedGoalGames: >= Fifty
        };

        var headToHeadQualification = _headToHeadData.Count <= 2 || _headToHeadData.OverScoredGames >= Fifty;

        return scoringPower switch
        {
            { Home: > Sixty, Away: > Sixty } when homeQualification && awayQualification && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg}" },

            { Home: > Sixty, Away: > Sixty } when (homeQualification || awayQualification) && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg}" },

            { Home: > 0.30, Away: > Sixty } when homeQualification && awayQualification && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} home has low chances" },

            { Home: > Sixty, Away: > 0.30 } when homeQualification && awayQualification && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} away has low chances" },

            { Home: > Sixty, Away: > Sixty } when homeQualification && awayQualification =>
                prediction with { Qualified = true, Msg = $"{msg} head to head ignored", HeadToHeadIgnored = true },


            { Home: > 0.28, Away: > Sixty } when (homeQualification || awayQualification) && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} home has low chances" },

            { Home: > Sixty, Away: > 0.28 } when (homeQualification || awayQualification) && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} away has low chances" },
            
            { Total: > 0.50 } when homeQualification && awayQualification =>
                prediction with { Qualified = true, Msg = $"{msg} head to head ignored", HeadToHeadIgnored = true },

            _ => prediction
        };
    }

    private double GetWeightedProbabilityBy(BetType betType)
    {
       var weightFactor = _headToHeadData.Count < 2 ? 0.50 : 0.35;
        const double weightFactorForH2H =  0.30;
        const double twentyFactor =  0.20;
        const double thirtyFactor =  0.30;
        var scoreProbability = _homeTeamData.GetScoreProbability(_awayTeamData);
        var bothScoreGoalProbability = _homeTeamData.GetBothScoredGameProbability(_awayTeamData);
        var overTwoGoalProbability = _homeTeamData.GetOverTwoGoalGameProbability(_awayTeamData);
        var overThreeGoalProbability = _homeTeamData.GetOverThreeGoalGameProbability(_awayTeamData);
        var underTwoGoalProbability = _homeTeamData.GetUnderTwoGoalGameProbability(_awayTeamData);
        var twoToThreeGoalGameProbability = _homeTeamData.GetUnderTwoGoalGameProbability(_awayTeamData);

        // Team score goal games, both team scored games 
        return betType switch
        {
            BetType.BothTeamScoreGoals => scoreProbability.Total * weightFactor + bothScoreGoalProbability.Total * weightFactor +
                            (_headToHeadData.Count < 2 ? 0 : _headToHeadData.BothTeamScoredGames * thirtyFactor),
            
            BetType.TwoToThreeGoals =>  scoreProbability.Total * weightFactor + twoToThreeGoalGameProbability * weightFactor +
                        (_headToHeadData.Count < 2 ? 0 : _headToHeadData.TwoToThreeGoalsGames * thirtyFactor),
            
            BetType.UnderThreeGoals => scoreProbability.Total * weightFactor + underTwoGoalProbability * weightFactor +
                                (_headToHeadData.Count < 2 ? 0 : _headToHeadData.UnderTwoScoredGames * thirtyFactor),
            
            BetType.OverTwoGoals => scoreProbability.Total * weightFactor + overTwoGoalProbability * weightFactor +
                                    (_headToHeadData.Count < 2 ? 0 : _headToHeadData.OverScoredGames * thirtyFactor),
            
            BetType.OverThreeGoals => scoreProbability.Total * weightFactor + overThreeGoalProbability * weightFactor +
                                      (_headToHeadData.Count < 2 ? 0 : _headToHeadData.OverThreeGoalGames * thirtyFactor),
            
            BetType.HomeWin =>  (_homeTeamData.WinAvg * 0.25 + _homeTeamData.HomeTeamWon * 0.25 + 
                                 _homeTeamData.ScoringPower.GetValueOrDefault() * 0.25 + 
                                 _homeTeamData.HomeScoringPower.GetValueOrDefault() * 0.25) * weightFactor +
                                scoreProbability.Total +
                                (_headToHeadData.Count < 2 ? 0 : _headToHeadData.HomeTeamWon * weightFactorForH2H),
            
            _ =>  (_awayTeamData.WinAvg * 0.25 + _awayTeamData.AwayTeamWon * 0.25 + _awayTeamData.ScoringPower.GetValueOrDefault() * 0.25 +
                      _awayTeamData.AwayScoringPower.GetValueOrDefault() * 0.25) * weightFactor +
                  scoreProbability.Total +
                  (_headToHeadData.Count < 2 ? 0 : _headToHeadData.AwayTeamWon * weightFactorForH2H)
        };
    }

    private Prediction CurrentTwoToThreeGoalChances()
    {
        var msg = string.Empty;
        var scoringPower = _homeTeamData.GetScoreProbability(_awayTeamData);
        var prediction = new Prediction(scoringPower.Total, BetType.TwoToThreeGoals);

        if (_homeTeamData.LastThreeMatchResult == BetType.TwoToThreeGoals)
            msg = "Risky: home last three two to three goal game";

        if (_awayTeamData.LastThreeMatchResult == BetType.TwoToThreeGoals)
            msg = "Risky: away last three two to three goal game";

        if (_homeTeamData.HomeScoringPower >= 0.70 && _awayTeamData.AwayScoringPower >= 0.70)
            return prediction with { Qualified = false, Msg = "Could be over 3 goals"};

        var homeQualification = _homeTeamData is
        {
            TwoToThreeGoalsGames: >= Fifty,
            TeamScoredGames: >= Fifty,
            TeamAllowedGoalGames: >= Fifty,
            OverThreeGoalGamesAvg: <= 0.40
        };

        var awayQualification = _awayTeamData is
        {
            TwoToThreeGoalsGames: >= Fifty,
            TeamScoredGames: >= Fifty,
            TeamAllowedGoalGames: >= Fifty,
            OverThreeGoalGamesAvg: <= 0.40
        };

        var headToHeadQualification = _headToHeadData.Count <= 2 || _headToHeadData is { TwoToThreeGoalsGames: >= Fifty, OverThreeGoalGames: >= 0.50 };

        if (_headToHeadData is { TwoToThreeGoalsGames: > 0.60, UnderTwoScoredGames: < 0.34 } &&
            _awayTeamData is { OverThreeGoalGamesAvg: <= 0.34, UnderTwoScoredGames: <= 0.34 } &&
            _homeTeamData is { OverThreeGoalGamesAvg: <= 0.34, UnderTwoScoredGames: <= 0.34 } && scoringPower.Total < 0.30)
        {
            return prediction with { Qualified = true, Msg = $"{msg}" };
        }
        
        return scoringPower switch
        {
            { Home: > Sixty, Away: > Sixty } when homeQualification && awayQualification && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg}" },

            { Home: > Sixty, Away: > Sixty } when (homeQualification || awayQualification) && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg}" },

            { Home: > 0.30, Away: > Sixty } when homeQualification && awayQualification && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} home has low chances" },

            { Home: > Sixty, Away: > 0.30 } when homeQualification && awayQualification && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} away has low chances" },

            { Home: > Sixty, Away: > Sixty } when homeQualification && awayQualification =>
                prediction with { Qualified = true, Msg = $"{msg} head to head ignored" },


            { Home: > 0.28, Away: > Sixty } when (homeQualification || awayQualification) && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} home has low chances" },

            { Home: > Sixty, Away: > 0.28 } when (homeQualification || awayQualification) && headToHeadQualification =>
                prediction with { Qualified = true, Msg = $"{msg} away has low chances" },
            
            { Total: > 0.50 } when homeQualification && awayQualification =>
                prediction with { Qualified = true, Msg = $"{msg} head to head ignored" },

            _ => prediction
        };
    }
        
    
    private (bool Qualified, double Percentage) CurrentUnderThreeGoalChances()
    {
        const double threshold = 0.59;
        const double goalThreshold = 0.80;
        const double moreThanThreeGoalsTolerance = 0.40;
        var probability = GetWeightedProbabilityBy(BetType.UnderThreeGoals);
        if (_homeTeamData.OverThreeGoalGamesAvg >= moreThanThreeGoalsTolerance &&
            _awayTeamData.OverThreeGoalGamesAvg >= moreThanThreeGoalsTolerance)
            return (false, probability);

        switch (_homeTeamData.UnderTwoScoredGames)
        {
            case >= threshold when _awayTeamData.UnderTwoScoredGames >= goalThreshold &&
                                   _headToHeadData.UnderTwoScoredGames >= goalThreshold &&
                                   _awayTeamData.OverThreeGoalGamesAvg < moreThanThreeGoalsTolerance &&
                                   _homeTeamData.OverThreeGoalGamesAvg < moreThanThreeGoalsTolerance:
                
            case >= threshold when _awayTeamData.UnderTwoScoredGames >= threshold &&
                                   _headToHeadData.UnderTwoScoredGames >= threshold &&
                                   _awayTeamData.OverThreeGoalGamesAvg < moreThanThreeGoalsTolerance &&
                                   _homeTeamData.OverThreeGoalGamesAvg < moreThanThreeGoalsTolerance:
                return (true, probability);
        }

        if ((_headToHeadData.UnderTwoScoredGames >= threshold &&
             (_homeTeamData.UnderTwoScoredGames >= goalThreshold || _awayTeamData.UnderTwoScoredGames >= goalThreshold)) ||
             (_homeTeamData.UnderTwoScoredGames >= threshold && _awayTeamData.UnderTwoScoredGames >= threshold)
            
            )
        {
            return (true, probability);
        }

        return (false, probability);
    }

        
    
    private Prediction CurrentWinChances()
    {
        var msg = string.Empty;
        var scoringPower = _homeTeamData.GetScoreProbability(_awayTeamData);
        var prediction = new Prediction(scoringPower.Home, BetType.HomeWin);

        if (_homeTeamData.LastThreeMatchResult == BetType.HomeWin)
            msg = "Risky: home won last three game";

        if (_awayTeamData.LastThreeMatchResult == BetType.AwayWin)
            msg = "Risky: away won last three game";

        if (scoringPower is { Home: <= 0.30, Away: <= 0.30 })
            return prediction with { Qualified = false, Msg = "Could be over 3 goals"};

        if (scoringPower.Home > scoringPower.Away &&
            _homeTeamData.HomeScoringPower > _awayTeamData.AwayScoringPower)
        {
            return prediction with { Qualified = true, Msg = "Home will win", Type = BetType.HomeWin, IsHome = true };
        }
        
        if (scoringPower.Away > scoringPower.Home &&
            _awayTeamData.AwayTeamWon > _homeTeamData.HomeTeamWon)
        {
            return prediction with { Qualified = true, Msg = "Away will win", Type = BetType.AwayWin, AwayPercentage = scoringPower.Away, IsHome = false };
        }  
      
        return prediction;
    }

    
    private PoissonProbability CalculatePoissonProbability(string home, string away, string playedOn, bool currentState = false)
    {
        var homeMatches = _historicalMatches.GetMatchesBy(home, playedOn).ToList();
        var awayMatches = _historicalMatches.GetMatchesBy(away, playedOn).ToList();
    
        if (currentState)
        {
            homeMatches = _historicalMatches.GetMatchesBy(home, playedOn).Take(5).ToList();
            awayMatches = _historicalMatches.GetMatchesBy(away, playedOn).Take(5).ToList();
        }
        
        var homeProbability = _poissonService.GetProbabilityBy(home,true, false, homeMatches);
        var awayProbability = _poissonService.GetProbabilityBy(away,false, false, awayMatches);
        
        var poisson = new PoissonProbability(homeProbability, awayProbability);
    
        return poisson;
    }
  
    private double GetWinAvg(TeamData teamData, bool atHome = true)
    {
        var result = teamData.HomeTeamWon * 0.25 + teamData.WinAvg * 0.25 + teamData.HomeTeamWon * 0.25 + _current.Home * 0.25;

        if (_headToHeadData.Count < 2)
            result = teamData.HomeTeamWon * 0.35 + teamData.WinAvg * 0.35 + _current.Home * 0.30;

        if (!atHome)
        {
            result = teamData.AwayTeamWon * 0.25 + teamData.WinAvg * 0.25 + teamData.AwayTeamWon * 0.25 + _current.Away * 0.25;

            if (_headToHeadData.Count < 2)
                result = teamData.AwayTeamWon * 0.35 + teamData.WinAvg * 0.35 + _current.Away * 0.30;
        }
        
        return result;
    }
}


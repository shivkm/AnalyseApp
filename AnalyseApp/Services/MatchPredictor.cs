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

    private PoissonProbability _season = default!;
    private PoissonProbability _current = default!;
    private HeadToHeadData _headToHeadData = default!;
    private TeamData _homeTeamData = default!;
    private TeamData _awayTeamData = default!;
    private const string OverTwoGoals = "Over Tow Goals";
    private const string UnderThreeGoals = "Under Three Goals";
    private const string BothTeamScore = "Both Team Score Goals";
    private const string TwoToThreeGoals = "Two to three Goals";
    private const string HomeWin = "Home will win";
    private const string AwayWin = "Away will win";
    
    public MatchPredictor(IFileProcessor fileProcessor, IPoissonService poissonService, IDataService dataService)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
        _poissonService = poissonService;
        _dataService = dataService;
        _fileProcessor = fileProcessor;
    }

    public void Execute()
    {
        var upcoming = _fileProcessor.GetUpcomingGames();
        SelfCheck();
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
    
    public void SelfCheck()
    {
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var teams = Enum.GetValues<PremierLeague>().ToList();
        var premierLeague = _historicalMatches.Where(i => i.Div == "E0").ToList();
        
        Console.WriteLine("--------------------- Premier League ----------------------------");
        foreach (var team in teams)
        {
            var teamPastGames = premierLeague.Where(item =>
                item.HomeTeam == team.GetDescription() || item.AwayTeam == team.GetDescription())
                .OrderByDescending(o => DateTime.Parse(o.Date))
                .Take(2);

            foreach (var teamPastGame in teamPastGames)
            {
                if (teamPastGame.HomeTeam == "Tottenham")
                {
                    
                }
                var prediction = Execute(teamPastGame.HomeTeam, teamPastGame.AwayTeam, teamPastGame.Date);
                //var isCorrect = CorrectCount(prediction, teamPastGame);

                if (prediction.Msg == "noHeadToHead")
                    continue;
                
                var isCorrect = teamPastGame.FTAG + teamPastGame.FTHG > 2 && prediction.Qualified ||
                                teamPastGame is { FTAG: > 0, FTHG: > 0 } && prediction.Msg == BothTeamScore;
                if (isCorrect)
                {
                    correctCount++;
                }
                else
                {
                    wrongCount++;
                }

                Console.WriteLine($"{teamPastGame.Date} - {teamPastGame.HomeTeam}:{teamPastGame.AwayTeam} {teamPastGame.FTAG + teamPastGame.FTHG} {prediction.Msg}");
                totalCount++;
            }
        }
        Console.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount}");
    }

    public Prediction Execute(string home, string away, string playedOn)
    {
        InitializeData(home, away, playedOn);
        var scoringPower = CurrentScoringPower();
        var noHeadToHead = _headToHeadData.Count < 2;
        var bothTeamScoreGoal = CurrentBothTeamScoreGoalChances();
        
        if (scoringPower)
        {
            // Predict Over or both team score
            if (bothTeamScoreGoal)
            {
                return new Prediction(BothTeamScore, true);
            }
            return new Prediction("", true);
        }
        
        return noHeadToHead ? new Prediction("noHeadToHead", false) : new Prediction("", false);
        
        
        var prediction = new Prediction("", false);

        if (_current.Home is double.NaN || _current.Away is double.NaN)
            return new Prediction("", false);

        var predictOdd = _headToHeadData.Count > 1 &&
                         (_homeTeamData.WinAvg >= 0.57 || _headToHeadData.HomeTeamWon >= 0.50 ||
                          _awayTeamData.WinAvg >= 0.57 || _headToHeadData.AwayTeamWon >= 0.50) ;

        // teams should be in more then neutral level in current state
        if (_season is { Home: > 0.68, Away: > 0.68 } || _current is { Home: > 0.60, Away: > 0.60 })
        {
            if (predictOdd)
            {
                prediction = PredictOddResult(prediction, true, false);
                if (prediction.Qualified) return prediction;
            }
            
            prediction = PredictOverTwoGoalsBy(prediction, true);
            if (prediction.Qualified) return prediction;
            
            prediction = PredictUnderScoreGames(prediction, true);
            if (prediction.Qualified) return prediction;
            
            prediction = PredictionBothTeamScoreGoals(prediction, true);
            if (prediction.Qualified) return prediction;
            
            PredictOddResult(prediction, true, true);
            if (prediction.Qualified) return prediction;
        }
        else
        {
            if (predictOdd)
            {
                prediction = PredictOddResult(prediction, false, false);
                if (prediction.Qualified) return prediction;
            }
            
            prediction = PredictOverTwoGoalsBy(prediction, false);
            if (prediction.Qualified) return prediction;
            
            prediction = PredictUnderScoreGames(prediction, false);
            if (prediction.Qualified) return prediction;
            
            prediction = PredictionBothTeamScoreGoals(prediction, false);
            if (prediction.Qualified) return prediction;
            
            prediction = PredictUnPredictedGames(prediction, false);
            if (prediction.Qualified) return prediction;

            PredictOddResult(prediction, false, true);
            if (prediction.Qualified) return prediction;
        }
        return prediction;
    }

    
    private void InitializeData(string home, string away, string playedOn)
    {
        _headToHeadData = _dataService.GetHeadToHeadDataBy(home, away, playedOn);
        _homeTeamData = _dataService.GetTeamDataBy(home, _historicalMatches);
        _awayTeamData = _dataService.GetTeamDataBy(away, _historicalMatches);
        _season = CalculatePoissonProbability(home, away, playedOn);
        _current = CalculatePoissonProbability(home, away, playedOn, true);
    }
    
    private bool CurrentScoringPower()
    {
        const double scoringThreshold = 0.60;
        const double toleranceThreshold = 0.30;
        
        return _current is 
            {
                Home: >= scoringThreshold, Away: >= toleranceThreshold
            } or
            {
                Away: >= scoringThreshold, Home: >= toleranceThreshold
            };
    }
    
    private bool CurrentBothTeamScoreGoalChances()
    {
        const double threshold = 0.59;
        var scoring = GetBothScoreGoalAvg();
        return _homeTeamData.BothTeamScoredGames >= threshold && _awayTeamData.BothTeamScoredGames >= threshold &&
               (_headToHeadData.BothTeamScoredGames >= threshold || _current is { Home: >= threshold, Away: >= threshold });
    }
    
    private Prediction PredictOverTwoGoalsBy(Prediction prediction, bool teamsInGoodForm)
    {
        var overTwoGoals = HasOverTwoGoalSuggestion(out var purePrediction);
        var overTwoGoal = GetOverTwoGoalAvg();
        var underThreeGoals = GetTwoToThreeGoalsAvg();
        var underTwoGoals = GetUnderThreeGoalsAvg();
        var bothScore = GetBothScoreGoalAvg();
        var scoringPower = _current.Home * 0.50 + _current.Away * 0.50;
        if (overTwoGoals && teamsInGoodForm && (purePrediction || _headToHeadData is { Count: > 3, OverScoredGames: >= 50 }))
        {
            return new Prediction(OverTwoGoals + 1, true);
        }
        // If teams are in good form and one of those team has 4 out of 7 over 2 goal games or
        // both teams should have over 2 goals games
        
        // Could be 0:0 games
        if (_homeTeamData.ZeroZeroGames >= 0.20 || _awayTeamData.ZeroZeroGames >= 0.20)
            return prediction;
        
        if ((teamsInGoodForm && 
             ((_homeTeamData is { OverScoredGames: >= 0.57, UnderScoredGames: < 0.57 } && _awayTeamData is { OverScoredGames: >= 0.28, UnderScoredGames: < 0.57 }) ||
              _awayTeamData  is { OverScoredGames: >= 0.57, UnderScoredGames: < 0.57 } && _homeTeamData is { OverScoredGames: >= 0.28, UnderScoredGames: < 0.57 })) ||
              _homeTeamData.OverScoredGames >= 0.57 && _awayTeamData.OverScoredGames >= 0.57)
        {
            // Suggestion is also indicating that it will be over 2 goals
            if (_homeTeamData.Suggestion is { Name: "OverScoredGames", Value: >= 0.57 } ||
                _awayTeamData.Suggestion is { Name: "OverScoredGames", Value: >= 0.57 })
            {
                return new Prediction(OverTwoGoals + 2, true);
            }
        }
        return prediction;
    }

    private Prediction PredictUnderScoreGames(Prediction prediction, bool teamsInGoodForm)
    {       
        var underThreeGoal = HasUnderThreeGoalSuggestion();    
        var underThreeGoals = GetUnderThreeGoalsAvg();
        var twoToThreeGoals = GetTwoToThreeGoalsAvg();
        var bothScores = GetBothScoreGoalAvg();
        var overTwoGoals = GetOverTwoGoalAvg();
        if (underThreeGoal && twoToThreeGoals > underThreeGoals && twoToThreeGoals > bothScores && twoToThreeGoals > overTwoGoals)
        {
            return new Prediction(TwoToThreeGoals, true);
        }
        if (underThreeGoal && teamsInGoodForm && (_headToHeadData.Count < 2 || _headToHeadData is { Count: > 3, UnderScoredGames: >= 50 }))
        {
            return new Prediction(UnderThreeGoals + 1, true);
        }
        
        // If teams are in good form and one of those team has 4 out of 7 under 3 goal games or
        // both teams should have under 3 goals games
        if ((teamsInGoodForm &&
             (_homeTeamData.UnderScoredGames >= 0.57 || _awayTeamData.UnderScoredGames >= 0.57)) ||
            _homeTeamData.UnderScoredGames >= 0.57 && _awayTeamData.UnderScoredGames >= 0.57)
        {
            if (_homeTeamData.UnderScoredGames >= 0.70 && _awayTeamData.UnderScoredGames >= 0.70 && _headToHeadData.UnderScoredGames >= 0.70)
            {
                return new Prediction(UnderThreeGoals + 2, true);
            }
            // Two to three goals are more promising
            var hasTwoToThreeSuggestion = HasTwoToThreeSuggestion();
            if (hasTwoToThreeSuggestion &&
                _homeTeamData.Suggestion is { Name: "TwoToThreeGoalsGames", Value: > 0.70 } ||
                 _homeTeamData.Suggestion is { Name: "UnderScoredGames", Value: < 0.57 } ||
                _awayTeamData.Suggestion is { Name: "TwoToThreeGoalsGames", Value: > 0.70 } ||
                 _awayTeamData.Suggestion is { Name: "UnderScoredGames", Value: < 0.57 })
            {
                return new Prediction(TwoToThreeGoals + 2, true);
            }
            
            // Suggestion is also indicating that it will be under 3 goals
            if (_homeTeamData.Suggestion is { Name: "UnderScoredGames", Value: > 0.50 } ||
                _awayTeamData.Suggestion is { Name: "UnderScoredGames", Value: > 0.50 })
            {
                return new Prediction(UnderThreeGoals + 3, true);
            }
        }

        // One team has worst performance
        if (_homeTeamData.UnderScoredGames >= 0.80 || _awayTeamData.UnderScoredGames >= 0.80)
        {
            if (_headToHeadData.Count < 2 && (_homeTeamData is { OverScoredGames: >= 0.57, HomeTeamWon: < 0.1 } ||
                                              _awayTeamData is { OverScoredGames: >= 0.57, AwayTeamWon: < 0.1 }))
            {
                return new Prediction(UnderThreeGoals + 4, true);
            }
        }
        
        return prediction;
    }

    private bool HasTwoToThreeSuggestion()
    {
        var homeTwoToThreeScores = _homeTeamData.TwoToThreeGoalsGames >= _homeTeamData.OverScoredGames ||
                                     _homeTeamData.TwoToThreeGoalsGames >= _homeTeamData.BothTeamScoredGames ||
                                     _homeTeamData.TwoToThreeGoalsGames >= _homeTeamData.UnderScoredGames;
        
        var awayTwoToThreeScores = _awayTeamData.TwoToThreeGoalsGames >= _awayTeamData.OverScoredGames ||
                                     _awayTeamData.TwoToThreeGoalsGames >= _awayTeamData.BothTeamScoredGames ||
                                     _awayTeamData.TwoToThreeGoalsGames >= _awayTeamData.UnderScoredGames;
        
        var headToHeadTwoToThreeScores = _headToHeadData.TwoToThreeGoalsGames >= _headToHeadData.OverScoredGames ||
                                           _headToHeadData.TwoToThreeGoalsGames >= _headToHeadData.BothTeamScoredGames ||
                                           _headToHeadData.TwoToThreeGoalsGames >= _headToHeadData.UnderScoredGames;

        return  homeTwoToThreeScores && awayTwoToThreeScores && headToHeadTwoToThreeScores ||
                homeTwoToThreeScores && !awayTwoToThreeScores && headToHeadTwoToThreeScores ||
                awayTwoToThreeScores && !homeTwoToThreeScores && headToHeadTwoToThreeScores ||
                homeTwoToThreeScores && awayTwoToThreeScores && !headToHeadTwoToThreeScores;
    }
    
    private bool HasOverTwoGoalSuggestion(out bool purePrediction)
    {
        var homeOverTwoScores = _homeTeamData.OverScoredGames >= 0.50 &&
                                    _homeTeamData.OverScoredGames >= _homeTeamData.TwoToThreeGoalsGames ||
                                    _homeTeamData.OverScoredGames >= _homeTeamData.BothTeamScoredGames ||
                                    _homeTeamData.OverScoredGames >= _homeTeamData.UnderScoredGames;
        
        var awayOverTwoScores = _awayTeamData.OverScoredGames >= 0.50 &&
                                    _awayTeamData.OverScoredGames >= _awayTeamData.TwoToThreeGoalsGames ||
                                    _awayTeamData.OverScoredGames >= _awayTeamData.BothTeamScoredGames ||
                                    _awayTeamData.OverScoredGames >= _awayTeamData.UnderScoredGames;
        
        var headToHeadOverTwoScores = _headToHeadData.OverScoredGames >= 0.50 &&
                                          _headToHeadData.OverScoredGames >= _headToHeadData.TwoToThreeGoalsGames ||
                                          _headToHeadData.OverScoredGames >= _headToHeadData.BothTeamScoredGames ||
                                          _headToHeadData.OverScoredGames >= _headToHeadData.UnderScoredGames;

        if (homeOverTwoScores && awayOverTwoScores && headToHeadOverTwoScores)
        {
            purePrediction = true;
            return true;
        }

        purePrediction = false;
        return  homeOverTwoScores && !awayOverTwoScores && headToHeadOverTwoScores ||
                awayOverTwoScores && !homeOverTwoScores && headToHeadOverTwoScores ||
                homeOverTwoScores && awayOverTwoScores && !headToHeadOverTwoScores;
    }
    
    private bool HasUnderThreeGoalSuggestion()
    {
        var homeUnderThreeScores = _homeTeamData.UnderScoredGames >= _homeTeamData.TwoToThreeGoalsGames ||
                                        _homeTeamData.UnderScoredGames >= _homeTeamData.BothTeamScoredGames ||
                                        _homeTeamData.UnderScoredGames >= _homeTeamData.OverScoredGames;
        
        var awayUnderThreeScores = _awayTeamData.UnderScoredGames >= _awayTeamData.TwoToThreeGoalsGames ||
                                        _awayTeamData.UnderScoredGames >= _awayTeamData.BothTeamScoredGames ||
                                        _awayTeamData.UnderScoredGames >= _awayTeamData.OverScoredGames;
        
        var headToHeadUnderThreeScores = _headToHeadData.UnderScoredGames >= _headToHeadData.TwoToThreeGoalsGames ||
                                              _headToHeadData.UnderScoredGames >= _headToHeadData.BothTeamScoredGames ||
                                              _headToHeadData.UnderScoredGames >= _headToHeadData.OverScoredGames;

        if (homeUnderThreeScores && awayUnderThreeScores && headToHeadUnderThreeScores)
        {
            return true;
        }
        
        return  homeUnderThreeScores && !awayUnderThreeScores && headToHeadUnderThreeScores ||
                awayUnderThreeScores && !homeUnderThreeScores && headToHeadUnderThreeScores ||
                homeUnderThreeScores && awayUnderThreeScores && !headToHeadUnderThreeScores;
    }
    
    private bool BothScoreSuggestion(out bool purePrediction)
    {
        var homeBothTeamScores = _homeTeamData.BothTeamScoredGames >= 0.50 &&
                                 (_homeTeamData.BothTeamScoredGames >= _homeTeamData.OverScoredGames ||
                                  _homeTeamData.BothTeamScoredGames >= _homeTeamData.TwoToThreeGoalsGames ||
                                  _homeTeamData.BothTeamScoredGames >= _homeTeamData.UnderScoredGames);
        
        var awayBothTeamScores = _awayTeamData.BothTeamScoredGames >=  0.50 &&
                                 (_awayTeamData.BothTeamScoredGames >= _awayTeamData.OverScoredGames ||
                                  _awayTeamData.BothTeamScoredGames >= _awayTeamData.TwoToThreeGoalsGames ||
                                  _awayTeamData.BothTeamScoredGames >= _awayTeamData.UnderScoredGames);
        
        var headToHeadBothTeamScores = _headToHeadData.BothTeamScoredGames >= 0.50 &&
                                       (_headToHeadData.BothTeamScoredGames >= _headToHeadData.OverScoredGames ||
                                        _headToHeadData.BothTeamScoredGames >= _headToHeadData.TwoToThreeGoalsGames ||
                                        _headToHeadData.BothTeamScoredGames >= _headToHeadData.UnderScoredGames);
        
        if (homeBothTeamScores && awayBothTeamScores && headToHeadBothTeamScores)
        {
            purePrediction = true;
            return true;
        }

        purePrediction = false;
        
        return homeBothTeamScores && !awayBothTeamScores && headToHeadBothTeamScores ||
               awayBothTeamScores && !homeBothTeamScores && headToHeadBothTeamScores ||
               homeBothTeamScores && awayBothTeamScores && !headToHeadBothTeamScores;

    }
    
    private Prediction PredictionBothTeamScoreGoals(Prediction prediction, bool teamsInGoodForm)
    {
        var bothTeamScoreGoals = BothScoreSuggestion(out var purePrediction);
        if (bothTeamScoreGoals && (purePrediction || _awayTeamData.BothTeamScoredGames > 0.70 || _homeTeamData.BothTeamScoredGames > 0.70) && 
            _headToHeadData is { TwoToThreeGoalsGames: <= 0.50, UnderScoredGames: <= 0.50 })
        {
            return new Prediction($"{BothTeamScore} 1", true);
        }

        return prediction;
    }
    
    private Prediction PredictUnPredictedGames(Prediction prediction, bool teamsInGoodForm)
    {
        var homeBothTeamScores = Math.Abs(_homeTeamData.BothTeamScoredGames - _homeTeamData.OverScoredGames) <= 0.0 ||
                                 Math.Abs(_homeTeamData.BothTeamScoredGames - _homeTeamData.TwoToThreeGoalsGames) <= 0.0 ||
                                 Math.Abs(_homeTeamData.BothTeamScoredGames - _homeTeamData.UnderScoredGames) <= 0.0;
        
        var awayBothTeamScores = Math.Abs(_awayTeamData.BothTeamScoredGames - _awayTeamData.OverScoredGames) <= 0.0 ||
                                 Math.Abs(_awayTeamData.BothTeamScoredGames - _awayTeamData.TwoToThreeGoalsGames) <= 0.0 ||
                                 Math.Abs(_awayTeamData.BothTeamScoredGames - _awayTeamData.UnderScoredGames) <= 0.0;
        
        var headToHeadBothTeamScores = Math.Abs(_headToHeadData.BothTeamScoredGames - _headToHeadData.OverScoredGames) <= 0.0 ||
                                       Math.Abs(_headToHeadData.BothTeamScoredGames - _headToHeadData.TwoToThreeGoalsGames) <= 0.0 ||
                                       Math.Abs(_headToHeadData.BothTeamScoredGames - _headToHeadData.UnderScoredGames) <= 0.0;

        if (!teamsInGoodForm && (_headToHeadData.Count <= 2 || headToHeadBothTeamScores) &&
            (homeBothTeamScores || awayBothTeamScores))
        {
            var headToHeadMissed = _headToHeadData.Count < 1 ? "Risky no head to head data" : "";
            return new Prediction($"{headToHeadMissed} {BothTeamScore} 1", true);
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

    

    private Prediction PredictOddResult(Prediction prediction, bool teamsInGoodForm, bool nothingPredicted)
    {
        var homeWin = GetWinAvg(_homeTeamData);
        var awayWin = GetWinAvg(_awayTeamData, false);
        
        var underThreeGoals = GetUnderThreeGoalsAvg();
        var twoToThreeGoals = GetTwoToThreeGoalsAvg();
        var bothScores = GetBothScoreGoalAvg();
        var overTwoGoals = GetOverTwoGoalAvg();

        var test = Math.Abs(homeWin - overTwoGoals);
        if (homeWin > awayWin &&
            Math.Abs(homeWin - underThreeGoals) > 0.015 &&
            Math.Abs(homeWin - twoToThreeGoals) > 0.015 &&
            Math.Abs(homeWin - bothScores) > 0.015 &&
            Math.Abs(homeWin - overTwoGoals) > 0.015 &&
            (nothingPredicted || homeWin > bothScores && homeWin > overTwoGoals))
        {
            return new Prediction(HomeWin  + 1, true);
        }
        
        if (awayWin > homeWin && 
            Math.Abs(awayWin - underThreeGoals) > 0.015 &&
            Math.Abs(awayWin - twoToThreeGoals) > 0.015 &&
            Math.Abs(awayWin - bothScores) > 0.015 &&
            Math.Abs(awayWin - overTwoGoals) > 0.015 &&
            (nothingPredicted || awayWin > bothScores && awayWin > overTwoGoals))
        {
            return new Prediction(AwayWin  + 1, true);
        }
        
        // if ((teamsInGoodForm || _current.Away >= 0.68 && _current.Away > _current.Home && !teamsInGoodForm && _current.Home < 0.68) &&
        //     _awayTeamData.WinAvg > 0.30 && (_awayTeamData.WinAvg > _homeTeamData.WinAvg || _awayTeamData.WinAvg >= _homeTeamData.WinAvg) && _headToHeadData.AwayTeamWon > _headToHeadData.HomeTeamWon)
        // {
        //     return new Prediction(" Away will win", true);
        // }
        // if ((teamsInGoodForm || _current.Home >= 0.68 && _current.Home > _current.Away && !teamsInGoodForm && _current.Away < 0.68) &&
        //     _homeTeamData.WinAvg > 0.30 && (_homeTeamData.WinAvg > _awayTeamData.WinAvg || _homeTeamData.WinAvg >= _awayTeamData.WinAvg) && _headToHeadData.HomeTeamWon > _headToHeadData.AwayTeamWon)
        // {
        //     return new Prediction(" Home will win", true);
        // }
        return prediction;
    }
 
    private Prediction PredictBothTeamGoalsWithHeadToHeadBy(Prediction prediction)
    {
        if (prediction.Qualified)
            return prediction;
        
        if ((_headToHeadData.BothTeamScoredGames > 0.66 ||
             _headToHeadData is { Suggestion: { Name: "BothTeamScoredGames", Value: > 0.60 } }) &&
            _current is { Home: > 0.60, Away: > 0.60 })
        {
            //return PredictBothTeamGoalsBy(prediction);
        }

        return prediction;
    }
    private Prediction PredictBothTeamGoalsBy()
    {
        var prediction = new Prediction("", false);
        var bothTeamScoreGoals = GetBothScoreGoalAvg();
        if (_homeTeamData is { BothTeamScoredGames: >= 0.57, ScoreProbability: > 0.70 } &&
            _awayTeamData is { BothTeamScoredGames: >= 0.57, ScoreProbability: > 0.70 })
        {
            if (_homeTeamData?.Suggestion is { Name: "BothTeamScoredGames", Value: > 0.70 } ||
                _awayTeamData?.Suggestion is { Name: "BothTeamScoredGames", Value: > 0.70 })
            {
                prediction = new Prediction(BothTeamScore, true);
            }
        }

        return prediction;
    }
    
    private Prediction PredictTwoToThreeGoalsBy(Prediction prediction)
    {
        if (prediction.Qualified)
            return prediction;

        var canIgnoreSuggestion = _headToHeadData.Suggestion.Name != "TwoToThreeGoalsGame" &&
                                      _headToHeadData.Suggestion.Value <= _headToHeadData.TwoToThreeGoalsGames;
        
        if (_headToHeadData.TwoToThreeGoalsGames > 0.66 &&
            (canIgnoreSuggestion || _headToHeadData is { Suggestion: { Name: "BothTeamScoredGames", Value: > 0.60 } }))
        {
            if (_homeTeamData is { TwoToThreeGoalsGames: >= 0.57, ScoreProbability: > 0.70 } &&
                _awayTeamData is { TwoToThreeGoalsGames: >= 0.57, ScoreProbability: > 0.70 })
            {
                if (_homeTeamData?.Suggestion is { Name: "TwoToThreeGoalsGames", Value: > 0.70 } ||
                    _awayTeamData?.Suggestion is { Name: "TwoToThreeGoalsGames", Value: > 0.70 })
                {
                    prediction = new Prediction(TwoToThreeGoals  + 1, true);
                }
            }
        }

        if (_headToHeadData.Count < 3)
        {
            if (_current is { Home: < 0.60, Away: < 0.60 } &&
                (_homeTeamData.OverScoredGames < 0.57 || _awayTeamData.OverScoredGames < 0.57))
            {
                if (_homeTeamData?.Suggestion is { Name: "TwoToThreeGoalsGames", Value: > 0.55 } ||
                    _awayTeamData?.Suggestion is { Name: "TwoToThreeGoalsGames", Value: > 0.55 })
                {
                    prediction = new Prediction(TwoToThreeGoals  + 2, true);
                }
            }
        }

        return prediction;
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

    private double GetBothScoreGoalAvg()
    {
        var scoredGamesWeight = _headToHeadData.Count >= 2 ? 0.30 : 0.20;
        var headToHeadScoredGamesWeight = _headToHeadData.Count >= 2 
            ? _headToHeadData.BothTeamScoredGames * 0.40 
            : _current.Home * 0.20 + _current.Away * 0.20;

        
        var result = _homeTeamData.BothTeamScoredGames * scoredGamesWeight +
                        _awayTeamData.BothTeamScoredGames * scoredGamesWeight + 
                        headToHeadScoredGamesWeight;

        return result;
    }
    private double GetOverTwoGoalAvg()
    {
        var result = _homeTeamData.OverScoredGames * 0.30 +
                     _awayTeamData.OverScoredGames * 0.30 + 
                     _headToHeadData.OverScoredGames * 0.40;

        if (_headToHeadData.Count < 2)
        {
            result = _homeTeamData.OverScoredGames * 0.30 +
                     _awayTeamData.OverScoredGames * 0.30 +
                     _current.Home * 0.20 + _current.Away * 0.20;
        }

        return result;
    }
    
    private double GetUnderThreeGoalsAvg()
    {
        var result = _homeTeamData.UnderScoredGames * 0.30 +
                     _awayTeamData.UnderScoredGames  * 0.30 + 
                     _headToHeadData.UnderScoredGames * 0.40;

        if (_headToHeadData.Count < 2)
        {
            result = _homeTeamData.UnderScoredGames * 0.30 +
                     _awayTeamData.UnderScoredGames * 0.30 +
                     _current.Home * 0.20 + _current.Away * 0.20;
        }

        return result;
    }
    
    private double GetTwoToThreeGoalsAvg()
    {
        var result = _homeTeamData.TwoToThreeGoalsGames * 0.30 +
                           _awayTeamData.TwoToThreeGoalsGames * 0.30 +
                           _headToHeadData.TwoToThreeGoalsGames * 0.40;

        if (_headToHeadData.Count < 2)
        {
            result = _homeTeamData.TwoToThreeGoalsGames * 0.30 +
                     _awayTeamData.TwoToThreeGoalsGames * 0.30 +
                     _current.Home * 0.20 + _current.Away * 0.20;
        }

        return result;
    }
    
    private static bool CorrectCount(Prediction actual, Matches game)
    {
        var totalGoals = game.FTHG + game.FTAG;

        return actual.Msg switch
        {
            BothTeamScore => game is { FTHG: > 0, FTAG: > 0 },
            OverTwoGoals => totalGoals > 2,
            UnderThreeGoals => totalGoals < 3,
            TwoToThreeGoals => totalGoals is 3 or 2,
            HomeWin => game.FTHG > game.FTAG,
            AwayWin => game.FTAG > game.FTHG,
            _ => false
        };
    }

}


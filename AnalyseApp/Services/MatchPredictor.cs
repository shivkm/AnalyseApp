using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class MatchPredictor: IMatchPredictor
{   
    private readonly List<Matches> _historicalMatches;
    private readonly IPoissonService _poissonService;
    private readonly IDataService _dataService;

    private PoissonProbability _season = default!;
    private PoissonProbability _current = default!;
    private HeadToHeadData _headToHeadData = default!;
    private TeamData _homeTeamData = default!;
    private TeamData _awayTeamData = default!;
    private bool _qualified = default!;
    
    public MatchPredictor(List<Matches> historicalMatches, IPoissonService poissonService, IDataService dataService)
    {
        _historicalMatches = historicalMatches;
        _poissonService = poissonService;
        _dataService = dataService;
    }

    public Prediction Execute(string home, string away, string playedOn)
    {
        _headToHeadData = _dataService.GetHeadToHeadDataBy(home, away, playedOn);
        _homeTeamData = _dataService.GetTeamDataBy(home, _historicalMatches);
        _awayTeamData = _dataService.GetTeamDataBy(away, _historicalMatches);
        _season = CalculatePoissonProbability(home, away, playedOn);
        _current = CalculatePoissonProbability(home, away, playedOn, true);

        var prediction = new Prediction("", false);

        var homeCurrentForm = _current.Home - _season.Home;
        var awayCurrentForm = _current.Away - _season.Away;

        if (homeCurrentForm is double.NaN || awayCurrentForm is double.NaN)
            return new Prediction("No enough data for analysis", false);

        // teams should be in more then neutral level in current state
        if (_season is { Home: > 0.68, Away: > 0.68 } ||
            _current is { Home: > 0.60, Away: > 0.60 } || 
            homeCurrentForm > 0.05 && awayCurrentForm > 0.05
           )
        {
            prediction = PredictOverTwoGoalsBy(prediction, true);
            if (prediction.Qualified) return prediction;
            
            prediction = PredictUnderScoreGames(prediction, true);
            if (prediction.Qualified) return prediction;
            
        }
        else
        {
            prediction = PredictOverTwoGoalsBy(prediction, false);
            if (prediction.Qualified) return prediction;
            
            prediction = PredictUnderScoreGames(prediction, false);
            if (prediction.Qualified) return prediction;
            
            prediction = PredictUnPredictedGames(prediction, false);
            if (prediction.Qualified) return prediction;
            
        }
        return prediction;
    }

    private Prediction PredictOverTwoGoalsBy(Prediction prediction, bool teamsInGoodForm)
    {
        var overTwoGoals = HasOverTwoGoalSuggestion();
        if (overTwoGoals && teamsInGoodForm && _headToHeadData is { Count: > 3, UnderScoredGames: >= 50 })
        {
            return new Prediction(" Over 2 goals", true);
        }
        // If teams are in good form and one of those team has 4 out of 7 over 2 goal games or
        // both teams should have over 2 goals games
        if ((teamsInGoodForm && 
             (_homeTeamData.OverScoredGames >= 0.57 && _awayTeamData.OverScoredGames >= 0.20 ||
              _awayTeamData.OverScoredGames >= 0.57 && _homeTeamData.OverScoredGames >= 0.20)) ||
            _homeTeamData.OverScoredGames >= 0.57 && _awayTeamData.OverScoredGames >= 0.57)
        {
            // Suggestion is also indicating that it will be over 2 goals
            if (_homeTeamData.Suggestion is { Name: "OverScoredGames", Value: >= 0.57 } ||
                _awayTeamData.Suggestion is { Name: "OverScoredGames", Value: >= 0.57 })
            {
                return new Prediction(" Over 2 goals", true);
            }
        }
        return prediction;
    }

    private Prediction PredictUnderScoreGames(Prediction prediction, bool teamsInGoodForm)
    {       
        var underThreeGoal = HasUnderThreeGoalSuggestion();
        if (underThreeGoal && teamsInGoodForm && (_headToHeadData.Count < 2 || _headToHeadData is { Count: > 3, OverScoredGames: >= 50 }))
        {
            return new Prediction(" under 3 goals", true);
        }
        
        // If teams are in good form and one of those team has 4 out of 7 under 3 goal games or
        // both teams should have under 3 goals games
        if ((teamsInGoodForm && (_homeTeamData.UnderScoredGames >= 0.57 || _awayTeamData.UnderScoredGames >= 0.57)) ||
            _homeTeamData.UnderScoredGames >= 0.57 && _awayTeamData.UnderScoredGames >= 0.57)
        {
            // Two to three goals are more promising
            var hasTwoToThreeSuggestion = HasTwoToThreeSuggestion();
            if (hasTwoToThreeSuggestion &&
                _homeTeamData.Suggestion is { Name: "TwoToThreeGoalsGames", Value: > 0.70 } ||
                 _homeTeamData.Suggestion is { Name: "UnderScoredGames", Value: < 0.57 } ||
                _awayTeamData.Suggestion is { Name: "TwoToThreeGoalsGames", Value: > 0.70 } ||
                 _awayTeamData.Suggestion is { Name: "UnderScoredGames", Value: < 0.57 })
            {
                return new Prediction(" Two to three goals", true);
            }
            
            // Suggestion is also indicating that it will be under 3 goals
            if (_homeTeamData.Suggestion is { Name: "UnderScoredGames", Value: > 0.50 } ||
                _awayTeamData.Suggestion is { Name: "UnderScoredGames", Value: > 0.50 })
            {
                return new Prediction(" Under 3 goals", true);
            }
        }
        
        return prediction;
        
        var canIgnoreHomeSuggestion = _homeTeamData.Suggestion.Name != "UnderScoredGames" &&
                                          _homeTeamData.Suggestion.Value <= _homeTeamData.UnderScoredGames;
        
        var canIgnoreAwaySuggestion = _awayTeamData.Suggestion.Name != "UnderScoredGames" &&
                                          _awayTeamData.Suggestion.Value <= _awayTeamData.UnderScoredGames;
        
        var canIgnoreHeadToHeadSuggestion = _headToHeadData.Suggestion.Name != "UnderScoredGames" &&
                                                _headToHeadData.Suggestion.Value <= _headToHeadData.UnderScoredGames;
        
        if (!prediction.Qualified && _headToHeadData is { Count: > 3, ScoreProbability: > 0.70, UnderScoredGames: >= 0.50 } && 
            (canIgnoreHeadToHeadSuggestion || _headToHeadData.Suggestion is { Name: "UnderScoredGames", Value: >= 0.50 })
            )
        {
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.60 } } &&
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.60 } })
            {
                prediction = new Prediction($"{prediction.Msg} under 3 goal", true);
            }
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.70 } } ||
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.70 } })
            {
                prediction = new Prediction($"{prediction.Msg} under 3 goal", true);
            }
        }

        // This will overwrite the head to head analysis because current forms indicate under 3 goal
        if (!prediction.Qualified && (_homeTeamData.UnderScoredGames >= 0.57 || _awayTeamData.UnderScoredGames >= 0.57))
        {
            if (_headToHeadData.UnderScoredGames >= 0.50)
            {
                if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.70 } } ||
                    _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.70 } })
                {
                    prediction = new Prediction($"{prediction.Msg} under 3 goal", true);
                }
            }
        }

        if (!prediction.Qualified && _headToHeadData.Count <= 2)
        {
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: >= 0.57 } } &&
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: >= 0.57 } })
            {
                prediction = new Prediction($"{prediction.Msg} under 3 goal", true);

            }
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: >= 0.70 } } ||
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: >= 0.70 } })
            {
                prediction = new Prediction($"{prediction.Msg} under 3 goal", true);
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
    
    private bool HasOverTwoGoalSuggestion()
    {
        var homeOverTwoScores = _homeTeamData.OverScoredGames >= _homeTeamData.TwoToThreeGoalsGames ||
                                 _homeTeamData.OverScoredGames >= _homeTeamData.BothTeamScoredGames ||
                                 _homeTeamData.OverScoredGames >= _homeTeamData.UnderScoredGames;
        
        var awayOverTwoScores = _awayTeamData.OverScoredGames >= _awayTeamData.TwoToThreeGoalsGames ||
                                 _awayTeamData.OverScoredGames >= _awayTeamData.BothTeamScoredGames ||
                                 _awayTeamData.OverScoredGames >= _awayTeamData.UnderScoredGames;
        
        var headToHeadOverTwoScores = _headToHeadData.OverScoredGames >= _headToHeadData.TwoToThreeGoalsGames ||
                                       _headToHeadData.OverScoredGames >= _headToHeadData.BothTeamScoredGames ||
                                       _headToHeadData.OverScoredGames >= _headToHeadData.UnderScoredGames;

        return  homeOverTwoScores && awayOverTwoScores && headToHeadOverTwoScores ||
                homeOverTwoScores && !awayOverTwoScores && headToHeadOverTwoScores ||
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

        return  homeUnderThreeScores && awayUnderThreeScores && headToHeadUnderThreeScores ||
                homeUnderThreeScores && !awayUnderThreeScores && headToHeadUnderThreeScores ||
                awayUnderThreeScores && !homeUnderThreeScores && headToHeadUnderThreeScores ||
                homeUnderThreeScores && awayUnderThreeScores && !headToHeadUnderThreeScores;
    }
    
    private bool BothScoreSuggestion()
    {
        var homeBothTeamScores = _homeTeamData.BothTeamScoredGames >= _homeTeamData.OverScoredGames ||
                                 _homeTeamData.BothTeamScoredGames >= _homeTeamData.TwoToThreeGoalsGames ||
                                 _homeTeamData.BothTeamScoredGames >= _homeTeamData.UnderScoredGames;
        
        var awayBothTeamScores = _awayTeamData.BothTeamScoredGames >= _awayTeamData.OverScoredGames ||
                                 _awayTeamData.BothTeamScoredGames >= _awayTeamData.TwoToThreeGoalsGames ||
                                 _awayTeamData.BothTeamScoredGames >= _awayTeamData.UnderScoredGames;
        
        var headToHeadBothTeamScores = _headToHeadData.BothTeamScoredGames >= _headToHeadData.OverScoredGames ||
                                           _headToHeadData.BothTeamScoredGames >= _headToHeadData.TwoToThreeGoalsGames ||
                                           _headToHeadData.BothTeamScoredGames >= _headToHeadData.UnderScoredGames;

        return  homeBothTeamScores && awayBothTeamScores && headToHeadBothTeamScores ||
                homeBothTeamScores && !awayBothTeamScores && headToHeadBothTeamScores ||
                awayBothTeamScores && !homeBothTeamScores && headToHeadBothTeamScores ||
                homeBothTeamScores && awayBothTeamScores && !headToHeadBothTeamScores;
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

        if (!teamsInGoodForm && (_headToHeadData.Count <= 2 || headToHeadBothTeamScores) && homeBothTeamScores && awayBothTeamScores)
        {
            return new Prediction($"{prediction.Msg} Both team score goal", true);
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


    private Prediction PredictOverTwoWithHeadToHeadGoalsBy(Prediction prediction)
    {
        if (_headToHeadData is
            {
                Suggestion:
                {
                    Name: "OverScoredGames", Value: > 0.60
                }
            } && _current is
            {
                Home: > 0.60, Away: > 0.60
            })
        {
            return PredictOverTwoGoalsBy(prediction, false);
        }

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
            return PredictBothTeamGoalsBy(prediction);
        }

        return prediction;
    }
    private Prediction PredictBothTeamGoalsBy(Prediction prediction)
    {
        if (prediction.Qualified)
            return prediction;
        
        if (_homeTeamData is { BothTeamScoredGames: >= 0.57, ScoreProbability: > 0.70 } &&
            _awayTeamData is { BothTeamScoredGames: >= 0.57, ScoreProbability: > 0.70 })
        {
            if (_homeTeamData?.Suggestion is { Name: "BothTeamScoredGames", Value: > 0.70 } ||
                _awayTeamData?.Suggestion is { Name: "BothTeamScoredGames", Value: > 0.70 })
            {
                prediction = new Prediction($"{prediction.Msg} both team score", true);
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
                    prediction = new Prediction($"{prediction.Msg} 2-3 goal", true);
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
                    prediction = new Prediction($"{prediction.Msg} 2-3 goal", true);
                }
            }
        }

        return prediction;
    }
    private string PredictTwoToThreeGoalsBy(string home, string away, string playedOn)
    {
        if (_current is { Home: < 0.50, Away: < 0.50 } &&
            _headToHeadData is { Count: < 3, TwoToThreeGoalsGames: 1 } &&
            (_homeTeamData is
             {
                 ScoreProbability: > 0.60,
                 OverScoredGames: < 0.50,
                 Suggestion: { Name: "TwoToThreeGoalsGames", Value: > 0.60 }
             } &&
             _awayTeamData is
             {
                 ScoreProbability: > 0.60,
                 OverScoredGames: < 0.50,
                 Suggestion: { Name: "UnderScoredGames", Value: > 0.60 }
             } ||
             _awayTeamData is
             {
                 ScoreProbability: > 0.60,
                 OverScoredGames: < 0.50,
                 Suggestion: { Name: "TwoToThreeGoalsGames", Value: > 0.60 }
             }) ||
            (_awayTeamData is
             {
                 ScoreProbability: > 0.60,
                 OverScoredGames: < 0.50,
                 Suggestion: { Name: "TwoToThreeGoalsGames", Value: > 0.60 }
             } &&
             (_awayTeamData is
              {
                  ScoreProbability: > 0.60,
                  OverScoredGames: < 0.50,
                  Suggestion: { Name: "UnderScoredGames", Value: > 0.60 }
              } ||
              _awayTeamData is
              {
                  ScoreProbability: > 0.60,
                  OverScoredGames: < 0.50,
                  Suggestion: { Name: "TwoToThreeGoalsGames", Value: > 0.60 }
              })))
        {
            _qualified = true;
            var msg = $"{playedOn} - {home}:{away} Two to three goals possible";
            Console.WriteLine(msg);
            return msg;
        }
        
        return "";
    }

    private string PredictSaveOverScoreBy(string home, string away, string playedOn)
    {
        if (_headToHeadData is { Count: > 3, Suggestion: { Name: "OverScoredGames", Value: > 0.60 } } && _current is { Home: < 0.50, Away: < 0.50 })
        {
            if (_homeTeamData is { OverScoredGames: > 0.50, ScoreProbability: > 0.70 } ||
                _awayTeamData is { OverScoredGames: > 0.50, ScoreProbability: > 0.70 })
            {
                if (_homeTeamData?.Suggestion is { Name: "OverScoredGames", Value: > 0.50 } ||
                    _awayTeamData?.Suggestion is { Name: "OverScoredGames", Value: > 0.50 })
                {
                    _qualified = true;
                    var msg = $"{playedOn} - {home}:{away} Over 2 goals possible";
                    Console.WriteLine(msg);
                    return msg;
                }
            }
        }

        return "";
    }
    
    private string PredictBothTeamScoreBy(string home, string away, string playedOn)
    {
        var msg = $"{playedOn} - {home}:{away} both team score goals";
        if (_headToHeadData is { Count: > 3, ScoreProbability: > 0.75, Suggestion: { Name: "BothTeamScoredGames", Value: > 0.60 } })
        {
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "BothTeamScoredGames", Value: > 0.50 } } &&
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "BothTeamScoredGames", Value: > 0.50 } })
            {
                _qualified = true;
                Console.WriteLine(msg);
                return msg;
            }
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "BothTeamScoredGames", Value: > 0.70 } } ||
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "BothTeamScoredGames", Value: > 0.70 } })
            {
                Console.WriteLine(msg);
                return msg;
            }
        }

        return "";
    }
    
    private Prediction PredictUnderScoreBy(Prediction prediction)
    {
        var canIgnoreHomeSuggestion = _homeTeamData.Suggestion.Name != "UnderScoredGames" &&
                                      _homeTeamData.Suggestion.Value <= _homeTeamData.UnderScoredGames;
        
        var canIgnoreAwaySuggestion = _awayTeamData.Suggestion.Name != "UnderScoredGames" &&
                                          _awayTeamData.Suggestion.Value <= _awayTeamData.UnderScoredGames;
        
        var canIgnoreHeadToHeadSuggestion = _headToHeadData.Suggestion.Name != "UnderScoredGames" &&
                                                _headToHeadData.Suggestion.Value <= _headToHeadData.UnderScoredGames;
        
        if (!prediction.Qualified && _headToHeadData is { Count: > 3, ScoreProbability: > 0.70, UnderScoredGames: >= 0.50 } && 
            (canIgnoreHeadToHeadSuggestion || _headToHeadData.Suggestion is { Name: "UnderScoredGames", Value: >= 0.50 })
            )
        {
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.60 } } &&
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.60 } })
            {
                prediction = new Prediction($"{prediction.Msg} under 3 goal", true);
            }
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.70 } } ||
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.70 } })
            {
                prediction = new Prediction($"{prediction.Msg} under 3 goal", true);
            }
        }

        // This will overwrite the head to head analysis because current forms indicate under 3 goal
        if (!prediction.Qualified && (_homeTeamData.UnderScoredGames >= 0.57 || _awayTeamData.UnderScoredGames >= 0.57))
        {
            if (_headToHeadData.UnderScoredGames >= 0.50)
            {
                if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.70 } } ||
                    _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: > 0.70 } })
                {
                    prediction = new Prediction($"{prediction.Msg} under 3 goal", true);
                }
            }
        }

        if (!prediction.Qualified && _headToHeadData.Count <= 2)
        {
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: >= 0.57 } } &&
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: >= 0.57 } })
            {
                prediction = new Prediction($"{prediction.Msg} under 3 goal", true);

            }
            if (_homeTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: >= 0.70 } } ||
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion: { Name: "UnderScoredGames", Value: >= 0.70 } })
            {
                prediction = new Prediction($"{prediction.Msg} under 3 goal", true);
            }
        }

        return prediction;
    }
    
    private string PredictOddBy(string home, string away, string playedOn)
    {
        var msg = string.Empty;
        if (_headToHeadData.Count <= 2)
        {
            var homePerformance = _current.Home - _season.Home;
            var awayPerformance = _current.Away - _season.Away;
            
            if (homePerformance > 0.1 && awayPerformance < 0 &&
                _homeTeamData is { ScoreProbability: > 0.70, Suggestion.Value: < 0.60 } &&
                _awayTeamData is { ScoreProbability: > 0.70, Suggestion.Value: < 0.60 })
            {
                if (_current.Home > _current.Away && _current.Home - _current.Away > 0.25 &&
                    _homeTeamData.WinAvg > 0.40 && _awayTeamData.WinAvg < 0.20 &&
                    _homeTeamData.HomeTeamWon > 0.25 && _awayTeamData.AwayTeamWon < 0.20 &&
                    _homeTeamData.HomeTeamWon - _awayTeamData.AwayTeamWon > 0.20)
                {
                    _qualified = true;
                    msg = $"{playedOn} - {home}:{away} home will win";
                    Console.WriteLine(msg);
                    return msg;
                }
                if (_current.Away > _current.Home && _current.Away - _current.Home > 0.25 &&
                    _homeTeamData.WinAvg < 0.20 && _awayTeamData.WinAvg > 0.40 &&
                    _homeTeamData.HomeTeamWon < 0.20 && _awayTeamData.AwayTeamWon > 0.25 &&
                    _awayTeamData.AwayTeamWon - _homeTeamData.HomeTeamWon > 0.20)
                {
                    _qualified = true;
                    msg = $"{playedOn} - {home}:{away} away will win";
                    Console.WriteLine(msg);
                    return msg;
                }
            }
            // Current performance indicate
            if (homePerformance < -0.25 || awayPerformance < -0.25)
            {
                if (_current.Home > _current.Away && _current.Home - _current.Away > 0.25 &&
                    _homeTeamData.WinAvg > 0.40 && _awayTeamData.WinAvg < 0.20 &&
                    _homeTeamData.HomeTeamWon > 0.25 && _awayTeamData.AwayTeamWon < 0.20 &&
                    _homeTeamData.HomeTeamWon - _awayTeamData.AwayTeamWon > 0.20)
                {
                    _qualified = true;
                    msg = $"{playedOn} - {home}:{away} home will win";
                    Console.WriteLine(msg);
                    return msg;
                }
                if (_current.Away > _current.Home && _current.Away - _current.Home > 0.25 &&
                    _homeTeamData.WinAvg < 0.20 && _awayTeamData.WinAvg > 0.40 &&
                    _homeTeamData.HomeTeamWon < 0.20 && _awayTeamData.AwayTeamWon > 0.25 &&
                    _awayTeamData.AwayTeamWon - _homeTeamData.HomeTeamWon > 0.20)
                {
                    _qualified = true;
                    msg = $"{playedOn} - {home}:{away} away will win";
                    Console.WriteLine(msg);
                    return msg;
                }
            }
        }
        return msg;
    }

    /// <summary>
    /// You can choose a statistical distribution and calculate the probability using its CDF.
    /// Here's an example using the Normal distribution.
    /// </summary>
    /// <param name="totalExpectedGoals"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public static double CalculateUnderProbability(double totalExpectedGoals, double threshold)
    {
        var standardDeviation = Math.Sqrt(totalExpectedGoals);
        var underProbability = NormalDistribution.CumulativeDistribution(threshold, totalExpectedGoals, standardDeviation);

        return underProbability;
    }
}


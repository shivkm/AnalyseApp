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
    private const string OverTwoGoals = "Over Two Goals";
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

    public Prediction Execute(string home, string away, string playedOn)
    {
        InitializeData(home, away, playedOn);
        var bothTeamScoreGoal = CurrentBothTeamScoreGoalChances();
        var overTwoGoals = CurrentOverTwoScoreGoalChances();
        var twoToThreeGoals = CurrentTwoToThreeGoalChances();
        var underThreeGoals = CurrentUnderThreeGoalChances();
        var winPredictions = CurrentWinChances();
        
        if (home == "Man United" || home == "Newcastle")
        {
            
        }
        
        if (overTwoGoals is { Qualified: true, Percentage: >= 0.50 }  &&
            BothTeamScoreGoalQualified(bothTeamScoreGoal, overTwoGoals) &&
            (!twoToThreeGoals.Qualified || twoToThreeGoals.Qualified && overTwoGoals.Percentage >= twoToThreeGoals.Percentage &&
            _homeTeamData.LastThreeMatchResult != BetType.TwoToThreeGoals && _awayTeamData.LastThreeMatchResult != BetType.TwoToThreeGoals) &&
            (!underThreeGoals.Qualified || underThreeGoals.Qualified && overTwoGoals.Percentage >= underThreeGoals.Percentage)
            )
        {
            return new Prediction(
                OverTwoGoals + $" {overTwoGoals.Percentage:F}%",
                true, overTwoGoals.Percentage, BetType.OverTwoGoals);
        }
        
        if (bothTeamScoreGoal is { Qualified: true, Percentage: >= 0.50 } &&
            (!twoToThreeGoals.Qualified ||
              twoToThreeGoals.Qualified && bothTeamScoreGoal.Percentage >= twoToThreeGoals.Percentage &&
             (_homeTeamData.LastThreeMatchResult == BetType.TwoToThreeGoals ||
              _awayTeamData.LastThreeMatchResult == BetType.TwoToThreeGoals))
            )
        {
            return new Prediction(BothTeamScore + $" {bothTeamScoreGoal.Percentage:F}%", 
                true, bothTeamScoreGoal.Percentage, BetType.BothTeamScoreGoals);
        }
        
        if (_homeTeamData.LastThreeMatchResult != BetType.TwoToThreeGoals &&
            _awayTeamData.LastThreeMatchResult != BetType.TwoToThreeGoals &&
            (twoToThreeGoals is { Qualified: true, Percentage: >= 0.50 } ||
             (twoToThreeGoals.Percentage >= 0.55 && !underThreeGoals.Qualified && winPredictions.Percentage < 0.55)))
        {
            return new Prediction(TwoToThreeGoals + $" {twoToThreeGoals.Percentage:F}%", 
                true, twoToThreeGoals.Percentage, BetType.TwoToThreeGoals);
        }
        
        if (underThreeGoals is { Qualified: true, Percentage: >= 0.50 } &&
            (!bothTeamScoreGoal.Qualified || bothTeamScoreGoal.Qualified && underThreeGoals.Percentage >= bothTeamScoreGoal.Percentage) &&
            (!twoToThreeGoals.Qualified || twoToThreeGoals.Qualified && underThreeGoals.Percentage >= twoToThreeGoals.Percentage) &&
            (!overTwoGoals.Qualified || overTwoGoals.Qualified && underThreeGoals.Percentage >= overTwoGoals.Percentage)
           )
        {
            return new Prediction(UnderThreeGoals + $" {underThreeGoals.Percentage:F}%", 
                true, underThreeGoals.Percentage, BetType.UnderThreeGoals);
        }

        if (winPredictions is { Qualified: true, isHome: true, Percentage: >= 0.50 })
        {
            return new Prediction(HomeWin, true, winPredictions.Percentage, BetType.HomeWin);
        }
        
        if (winPredictions is { Qualified: true, isHome: false, Percentage: >= 0.50 })
        {
            return new Prediction(AwayWin, true, winPredictions.Percentage, BetType.AwayWin);
        }
        
        // if the code reach this area that means all previous prediction failed to approve
        if (overTwoGoals.Percentage > bothTeamScoreGoal.Percentage &&
            overTwoGoals.Percentage > underThreeGoals.Percentage &&
            (overTwoGoals.Percentage > twoToThreeGoals.Percentage ||
             _homeTeamData.LastThreeMatchResult != BetType.TwoToThreeGoals || _awayTeamData.LastThreeMatchResult != BetType.TwoToThreeGoals))
        {
            return new Prediction(
                OverTwoGoals + $" risky: {overTwoGoals.Percentage:F}%",
                true, overTwoGoals.Percentage, BetType.OverTwoGoals);
        }
        
        if (bothTeamScoreGoal.Percentage > underThreeGoals.Percentage &&
            bothTeamScoreGoal.Percentage > twoToThreeGoals.Percentage)
        {
            return new Prediction(BothTeamScore + $" risky {bothTeamScoreGoal.Percentage:F}%", 
                true, bothTeamScoreGoal.Percentage, BetType.BothTeamScoreGoals);
        }    
        
        if (twoToThreeGoals.Percentage > underThreeGoals.Percentage)
        {
            return new Prediction(TwoToThreeGoals + $" risky {twoToThreeGoals.Percentage:F}%", 
                true, twoToThreeGoals.Percentage, BetType.TwoToThreeGoals);
        }
        
        return new Prediction(UnderThreeGoals + $" risky {underThreeGoals.Percentage:F}%", 
            true, underThreeGoals.Percentage, BetType.UnderThreeGoals);
    }

    private bool BothTeamScoreGoalQualified((bool Qualified, double Percentage) bothTeamScoreGoal, (bool Qualified, double Percentage) overTwoGoals)
    {
        return !bothTeamScoreGoal.Qualified ||
                bothTeamScoreGoal.Qualified &&
                overTwoGoals.Percentage >= bothTeamScoreGoal.Percentage && 
                _homeTeamData.LastThreeMatchResult != BetType.BothTeamScoreGoals && 
                _awayTeamData.LastThreeMatchResult != BetType.BothTeamScoreGoals;
    }

    private void InitializeData(string home, string away, string playedOn)
    {
        _headToHeadData = _dataService.GetHeadToHeadDataBy(home, away, playedOn);
        _homeTeamData = _dataService.GetTeamDataBy(home, _historicalMatches);
        _awayTeamData = _dataService.GetTeamDataBy(away, _historicalMatches);
        _season = CalculatePoissonProbability(home, away, playedOn);
        _current = CalculatePoissonProbability(home, away, playedOn, true);
    }
    
    private (bool Qualified, double Percentage) CurrentBothTeamScoreGoalChances()
    {
        const double threshold = 0.59;
        const double goalThreshold = 0.80;
        const double underThreeGoalsTolerance = 0.70;
        var probability = GetWeightedProbabilityBy(BetType.BothTeamScoreGoals);
        
        // if anything indicate under 3 goals then ignore the game
        if (_homeTeamData.UnderScoredGames > underThreeGoalsTolerance && _awayTeamData.UnderScoredGames > underThreeGoalsTolerance)
            return (false, probability);

        // Teams and head to heads says that both team score average is or above sixty percent
        if (_homeTeamData.BothTeamScoredGames >= threshold && (_homeTeamData.TeamScoredGames >= goalThreshold || _homeTeamData.TeamAllowedGoalGames > goalThreshold) &&
            _awayTeamData.BothTeamScoredGames >= threshold && (_awayTeamData.TeamScoredGames >= goalThreshold || _awayTeamData.TeamAllowedGoalGames > goalThreshold) &&
            _headToHeadData.BothTeamScoredGames >= threshold)
        {
            return (true, probability);
        }

        switch (_homeTeamData.TeamScoredGames)
        {
            // Teams allowed and scored at least one goal in recent games
            case >= goalThreshold when _awayTeamData.TeamAllowedGoalGames >= goalThreshold &&
                                       _homeTeamData.TeamAllowedGoalGames >= goalThreshold && _awayTeamData.TeamScoredGames >= goalThreshold:
                
            // Teams allowed and scored at least one goal in recent games with tolerance of one team allowing goal 
            case >= goalThreshold when _awayTeamData.TeamScoredGames >= goalThreshold &&
                                       (_homeTeamData.TeamAllowedGoalGames >= goalThreshold || _awayTeamData.TeamAllowedGoalGames >= goalThreshold):
                return (true, probability);
        }

        // Teams allowed and scored at least one goal in recent games with tolerance of one team scoring goal 
        if (_homeTeamData.TeamAllowedGoalGames >= goalThreshold &&
            _awayTeamData.TeamAllowedGoalGames >= goalThreshold &&
            (_homeTeamData.TeamScoredGames >= goalThreshold || _awayTeamData.TeamScoredGames >= goalThreshold))
        {
            return (true, probability);
        }
        
        if (
            (_homeTeamData.TeamAllowedGoalGames >= goalThreshold && _awayTeamData.TeamAllowedGoalGames >= 0.50 ||
             _homeTeamData.TeamAllowedGoalGames >= 0.50 && _awayTeamData.TeamAllowedGoalGames >= goalThreshold) &&
            (_homeTeamData.TeamScoredGames >= goalThreshold || _awayTeamData.TeamScoredGames >= goalThreshold) &&
            _homeTeamData.OverScoredGames <= _homeTeamData.BothTeamScoredGames && _awayTeamData.OverScoredGames <= _awayTeamData.BothTeamScoredGames)
        {
            return (true, probability);
        }
        
        return (false, probability);
    }
    
    private (bool Qualified, double Percentage) CurrentOverTwoScoreGoalChances()
    {
        const double threshold = 0.59;
        const double underThreeGoalsTolerance = 0.70;
        var probability = GetWeightedProbabilityBy(BetType.OverTwoGoals);
        
        // if anything indicate under 3 goals then ignore the game
        if (_homeTeamData.TeamScoreProbability < 0.40 || _awayTeamData.TeamScoreProbability < 0.40 && _homeTeamData.UnderScoredGames > underThreeGoalsTolerance && _awayTeamData.UnderScoredGames > underThreeGoalsTolerance)
            return (false, probability);
        
        switch (_homeTeamData.TeamScoredGames)
        {
            // Teams scored and conceded at least one goal in recent games and over two goals average is more than 60
            case >= threshold when _awayTeamData.TeamAllowedGoalGames >= threshold &&
                                   _homeTeamData.TeamAllowedGoalGames >= threshold && 
                                   _awayTeamData.TeamScoredGames >= threshold &&
                                   (_homeTeamData.OverScoredGames >= threshold ||
                                    _awayTeamData.OverScoredGames >= threshold) && 
                                   _headToHeadData.OverScoredGames >= threshold:
                
            // Teams scored and conceded at least one goal in recent games with tolerance of one team conceded goal and over two goals average is more than 60
            case >= threshold when _awayTeamData.TeamScoredGames >= threshold &&
                                   (_homeTeamData.TeamAllowedGoalGames >= threshold ||
                                    _awayTeamData.TeamAllowedGoalGames >= threshold) &&
                                   (_homeTeamData.OverScoredGames >= threshold && _headToHeadData.OverScoredGames >= threshold ||
                                    _awayTeamData.OverScoredGames >= threshold && _headToHeadData.OverScoredGames >= threshold):
                return (true, probability);
        }

        // Teams scored and conceded at least one goal in recent games with tolerance of one team conceded goal and over two goals average is more than 60
        if(_homeTeamData.TeamAllowedGoalGames >= threshold &&
               _awayTeamData.TeamAllowedGoalGames >= threshold &&
               (_homeTeamData.OverScoredGames > threshold || _awayTeamData.OverScoredGames > threshold || _headToHeadData.OverScoredGames > threshold) &&
               (_homeTeamData.TeamScoredGames >= threshold || _awayTeamData.TeamScoredGames >= threshold))
        {
            return (true, probability);
        }
        
        return (false, probability);
    }

    private double GetWeightedProbabilityBy(BetType betType)
    {
        var probability = betType switch
        {
            BetType.BothTeamScoreGoals => _headToHeadData.Count < 2
                ? _homeTeamData.BothTeamScoredGames * 0.25 + _homeTeamData.TeamScoreProbability * 0.25 +
                  _awayTeamData.BothTeamScoredGames * 0.25 + _awayTeamData.TeamScoreProbability * 0.25
                : _homeTeamData.BothTeamScoredGames * 0.20 + _homeTeamData.TeamScoreProbability * 0.20 +
                  _awayTeamData.BothTeamScoredGames * 0.20 + _awayTeamData.TeamScoreProbability * 0.20 +
                  _headToHeadData.BothTeamScoredGames * 0.20,
            BetType.TwoToThreeGoals => _headToHeadData.Count < 2
                ? _homeTeamData.TwoToThreeGoalsGames * 0.25 + _homeTeamData.TeamScoreProbability * 0.25 +
                  _awayTeamData.TwoToThreeGoalsGames * 0.25 + _awayTeamData.TeamScoreProbability * 0.25
                : _homeTeamData.TwoToThreeGoalsGames * 0.20 + _homeTeamData.TeamScoreProbability * 0.20 +
                  _awayTeamData.TwoToThreeGoalsGames * 0.20 + _awayTeamData.TeamScoreProbability * 0.20 +
                  _headToHeadData.TwoToThreeGoalsGames * 0.20,
            BetType.UnderThreeGoals => _headToHeadData.Count < 2
                ? _homeTeamData.UnderScoredGames * 0.25 + _homeTeamData.TeamScoreProbability * 0.25 +
                  _awayTeamData.UnderScoredGames * 0.25 + _awayTeamData.TeamScoreProbability * 0.25
                : _homeTeamData.UnderScoredGames * 0.20 + _homeTeamData.TeamScoreProbability * 0.20 +
                  _awayTeamData.UnderScoredGames * 0.20 + _awayTeamData.TeamScoreProbability * 0.20 +
                  _headToHeadData.UnderScoredGames * 0.20,
            _ => _headToHeadData.Count < 2
                ? _homeTeamData.OverScoredGames * 0.25 + _homeTeamData.TeamScoreProbability * 0.25 +
                  _awayTeamData.OverScoredGames * 0.25 + _awayTeamData.TeamScoreProbability * 0.25
                : _homeTeamData.OverScoredGames * 0.20 + _homeTeamData.TeamScoreProbability * 0.20 +
                  _awayTeamData.OverScoredGames * 0.20 + _awayTeamData.TeamScoreProbability * 0.20 +
                  _headToHeadData.OverScoredGames * 0.20
        };
        
        double weightFactor = _headToHeadData.Count < 2 ? 0.25 : 0.20;
        double probability2;

        switch (betType)
        {
            case BetType.BothTeamScoreGoals:
                probability2 = (_homeTeamData.BothTeamScoredGames + _awayTeamData.BothTeamScoredGames) * weightFactor +
                              (_homeTeamData.TeamScoreProbability + _awayTeamData.TeamScoreProbability) * weightFactor +
                              (_headToHeadData.Count < 2 ? 0 : _headToHeadData.BothTeamScoredGames * weightFactor);
                break;
            case BetType.TwoToThreeGoals:
                probability2 = (_homeTeamData.TwoToThreeGoalsGames + _awayTeamData.TwoToThreeGoalsGames) * weightFactor +
                              (_homeTeamData.TeamScoreProbability + _awayTeamData.TeamScoreProbability) * weightFactor +
                              (_headToHeadData.Count < 2 ? 0 : _headToHeadData.TwoToThreeGoalsGames * weightFactor);
                break;
            case BetType.UnderThreeGoals:
                probability2 = (_homeTeamData.UnderScoredGames + _awayTeamData.UnderScoredGames) * weightFactor +
                              (_homeTeamData.TeamScoreProbability + _awayTeamData.TeamScoreProbability) * weightFactor +
                              (_headToHeadData.Count < 2 ? 0 : _headToHeadData.UnderScoredGames * weightFactor);
                break;
            default:
                probability2 = (_homeTeamData.OverScoredGames + _awayTeamData.OverScoredGames) * weightFactor +
                              (_homeTeamData.TeamScoreProbability + _awayTeamData.TeamScoreProbability) * weightFactor +
                              (_headToHeadData.Count < 2 ? 0 : _headToHeadData.OverScoredGames * weightFactor);
                break;
        }

        
        
        return probability;
    }

    private (bool Qualified, double Percentage) CurrentTwoToThreeGoalChances()
    {
        const double threshold = 0.59;
        const double goalThreshold = 0.80;
        const double moreThanThreeGoalsTolerance = 0.50;
        var probability = GetWeightedProbabilityBy(BetType.TwoToThreeGoals);
        
        if (_homeTeamData.MoreThanThreeGoalGamesAvg >= moreThanThreeGoalsTolerance &&
            _awayTeamData.MoreThanThreeGoalGamesAvg >= moreThanThreeGoalsTolerance)
            return (false, probability);

        switch (_homeTeamData.TwoToThreeGoalsGames)
        {
            case >= threshold when _awayTeamData.TwoToThreeGoalsGames >= goalThreshold &&
                                   _headToHeadData.TwoToThreeGoalsGames >= goalThreshold &&
                                   _awayTeamData.MoreThanThreeGoalGamesAvg < moreThanThreeGoalsTolerance &&
                                   _homeTeamData.MoreThanThreeGoalGamesAvg < moreThanThreeGoalsTolerance:
                
            case >= threshold when _awayTeamData.TwoToThreeGoalsGames >= threshold &&
                                   _headToHeadData.TwoToThreeGoalsGames >= threshold &&
                                   _awayTeamData.MoreThanThreeGoalGamesAvg < moreThanThreeGoalsTolerance &&
                                   _homeTeamData.MoreThanThreeGoalGamesAvg < moreThanThreeGoalsTolerance:
                return (true, probability);
        }

        if ((_headToHeadData.TwoToThreeGoalsGames >= threshold &&
             (_homeTeamData.TwoToThreeGoalsGames >= goalThreshold ||
              _awayTeamData.TwoToThreeGoalsGames >= goalThreshold)) ||
             (_homeTeamData.TwoToThreeGoalsGames >= threshold && _awayTeamData.TwoToThreeGoalsGames >= threshold)
            
            )
        {
            return (true, probability);
        }
        
        if (_headToHeadData.TwoToThreeGoalsGames >= 0.50 && _homeTeamData.TwoToThreeGoalsGames >= 0.50 &&
            _awayTeamData.TwoToThreeGoalsGames >= 0.50 && probability > 0.80
           )
        {
            return (true, probability);
        }
        
        return (false, probability);
    }
        
    
    private (bool Qualified, double Percentage) CurrentUnderThreeGoalChances()
    {
        const double threshold = 0.59;
        const double goalThreshold = 0.80;
        const double moreThanThreeGoalsTolerance = 0.40;
        var probability = _headToHeadData.Count < 2 
            ? _homeTeamData.UnderScoredGames * 0.50 + _awayTeamData.UnderScoredGames * 0.50
            : _homeTeamData.UnderScoredGames * 0.35 + _awayTeamData.UnderScoredGames * 0.35 + _headToHeadData.UnderScoredGames * 0.30;
        
        if (_homeTeamData.MoreThanThreeGoalGamesAvg >= moreThanThreeGoalsTolerance &&
            _awayTeamData.MoreThanThreeGoalGamesAvg >= moreThanThreeGoalsTolerance)
            return (false, probability);

        switch (_homeTeamData.UnderScoredGames)
        {
            case >= threshold when _awayTeamData.UnderScoredGames >= goalThreshold &&
                                   _headToHeadData.UnderScoredGames >= goalThreshold &&
                                   _awayTeamData.MoreThanThreeGoalGamesAvg < moreThanThreeGoalsTolerance &&
                                   _homeTeamData.MoreThanThreeGoalGamesAvg < moreThanThreeGoalsTolerance:
                
            case >= threshold when _awayTeamData.UnderScoredGames >= threshold &&
                                   _headToHeadData.UnderScoredGames >= threshold &&
                                   _awayTeamData.MoreThanThreeGoalGamesAvg < moreThanThreeGoalsTolerance &&
                                   _homeTeamData.MoreThanThreeGoalGamesAvg < moreThanThreeGoalsTolerance:
                return (true, probability);
        }

        if ((_headToHeadData.UnderScoredGames >= threshold &&
             (_homeTeamData.UnderScoredGames >= goalThreshold || _awayTeamData.UnderScoredGames >= goalThreshold)) ||
             (_homeTeamData.UnderScoredGames >= threshold && _awayTeamData.UnderScoredGames >= threshold)
            
            )
        {
            return (true, probability);
        }

        return (false, probability);
    }

        
    
    private (bool Qualified, double Percentage, bool isHome) CurrentWinChances()
    {
        const double threshold = 0.50;
        var winHomeProbability = _headToHeadData.Count < 2 
            ? _homeTeamData.WinAvg * (threshold - 0.25) + _homeTeamData.HomeTeamWon * (threshold + 0.25)
            : _homeTeamData.WinAvg * 0.25 + _homeTeamData.HomeTeamWon * 0.40 + _headToHeadData.HomeTeamWon * 0.35;
        
        var winAwayProbability = _headToHeadData.Count < 2 
            ? _awayTeamData.WinAvg * (threshold - 0.25) + _awayTeamData.AwayTeamWon * (threshold + 0.25)
            : _awayTeamData.WinAvg * 0.25 + _awayTeamData.AwayTeamWon * 0.40 + _headToHeadData.AwayTeamWon * 0.35;

        if ((winHomeProbability - winAwayProbability >= 0.30 || winHomeProbability > winAwayProbability && _current.Home > _current.Away &&
             _current.Home - _current.Away > 0.15) &&
            (_homeTeamData.TeamScoredGames > _awayTeamData.TeamScoredGames ||
             _homeTeamData.TeamScoredGames >= _awayTeamData.TeamScoredGames &&
             _homeTeamData.TeamAllowedGoalGames < _awayTeamData.TeamAllowedGoalGames))
        {
            return (true, winHomeProbability, true);
        }
        
        if ((winAwayProbability - winHomeProbability >= 0.30 || winAwayProbability > winHomeProbability && _current.Away > _current.Home &&
             _current.Away - _current.Home > 0.15) &&
            (_awayTeamData.TeamScoredGames > _homeTeamData.TeamScoredGames ||
             _awayTeamData.TeamScoredGames >= _homeTeamData.TeamScoredGames &&
             _awayTeamData.TeamAllowedGoalGames < _homeTeamData.TeamAllowedGoalGames))
        {
            return (true, winAwayProbability, false);
        }

        return (false, 0.0, false);
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


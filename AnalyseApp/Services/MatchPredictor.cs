using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using Microsoft.Extensions.FileProviders;

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
        var bothTeamScoreGoal = CurrentBothTeamScoreGoalChances();
        var overTwoGoals = CurrentOverTwoScoreGoalChances();
        var twoToThreeGoals = CurrentTwoToThreeGoalChances();
        var underThreeGoals = CurrentUnderThreeGoalChances();
        if (home == "Swansea")
        {
            
        }
        
        if (overTwoGoals is { Qualified: true, Percentage: >= 0.50 }  && 
            overTwoGoals.Percentage >= bothTeamScoreGoal.Percentage &&
            overTwoGoals.Percentage >= twoToThreeGoals.Percentage &&
            overTwoGoals.Percentage > underThreeGoals.Percentage)
        {
            return new Prediction(OverTwoGoals + $" {overTwoGoals.Percentage:F}%", true, overTwoGoals.Percentage, BetType.OverTwoGoals);
        }
        
        if (bothTeamScoreGoal is { Qualified: true, Percentage: >= 0.50 } &&
            bothTeamScoreGoal.Percentage >= overTwoGoals.Percentage && 
            bothTeamScoreGoal.Percentage >= twoToThreeGoals.Percentage)
        {
            return new Prediction(BothTeamScore + $" {bothTeamScoreGoal.Percentage:F}%", true, bothTeamScoreGoal.Percentage, BetType.BothTeamScoreGoals);
        }
        
        if (twoToThreeGoals is { Qualified: true, Percentage: >= 0.50 } &&
            twoToThreeGoals.Percentage >= bothTeamScoreGoal.Percentage && 
            twoToThreeGoals.Percentage >= overTwoGoals.Percentage)
        {
            return new Prediction(TwoToThreeGoals + $" {twoToThreeGoals.Percentage:F}%", true, twoToThreeGoals.Percentage, BetType.TwoToThreeGoals);
        }
        
        if (underThreeGoals is { Qualified: true, Percentage: >= 0.50 } &&
            underThreeGoals.Percentage >= bothTeamScoreGoal.Percentage &&
            underThreeGoals.Percentage > overTwoGoals.Percentage && 
            underThreeGoals.Percentage > twoToThreeGoals.Percentage)
        {
            return new Prediction(UnderThreeGoals + $" {underThreeGoals.Percentage:F}%", true, underThreeGoals.Percentage, BetType.UnderThreeGoals);
        }
        
        return new Prediction("", false, underThreeGoals.Percentage, BetType.HomeWin);
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
        var probability = _headToHeadData.Count < 2 
            ? _homeTeamData.OverScoredGames * 0.50 + _awayTeamData.OverScoredGames * 0.50
            : _homeTeamData.OverScoredGames* 0.50 + _awayTeamData.OverScoredGames * 0.50 + _headToHeadData.OverScoredGames * 0.50;
        
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
        
        return (false, probability);
    }
    
    private (bool Qualified, double Percentage) CurrentOverTwoScoreGoalChances()
    {
        const double threshold = 0.59;
        const double underThreeGoalsTolerance = 0.70;
        var probability = _headToHeadData.Count < 2 
            ? _homeTeamData.OverScoredGames * 0.50 + _awayTeamData.OverScoredGames * 0.50
            : _homeTeamData.OverScoredGames* 0.50 + _awayTeamData.OverScoredGames * 0.50 + _headToHeadData.OverScoredGames * 0.50;
        
        // if anything indicate under 3 goals then ignore the game
        if (_homeTeamData.UnderScoredGames > underThreeGoalsTolerance && _awayTeamData.UnderScoredGames > underThreeGoalsTolerance)
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
    
    private (bool Qualified, double Percentage) CurrentTwoToThreeGoalChances()
    {
        const double threshold = 0.59;
        const double goalThreshold = 0.80;
        const double moreThanThreeGoalsTolerance = 0.50;
        var probability = _headToHeadData.Count < 2 
            ? _homeTeamData.TwoToThreeGoalsGames * 0.50 + _awayTeamData.TwoToThreeGoalsGames * 0.50
            : _homeTeamData.TwoToThreeGoalsGames * 0.50 + _awayTeamData.TwoToThreeGoalsGames * 0.50 + _headToHeadData.TwoToThreeGoalsGames * 0.50;
        
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
        
        return (false, probability);
    }
        
    
    private (bool Qualified, double Percentage) CurrentUnderThreeGoalChances()
    {
        const double threshold = 0.59;
        const double goalThreshold = 0.80;
        const double moreThanThreeGoalsTolerance = 0.40;
        var probability = _headToHeadData.Count < 2 
            ? _homeTeamData.UnderScoredGames * 0.50 + _awayTeamData.UnderScoredGames * 0.50
            : _homeTeamData.UnderScoredGames * 0.50 + _awayTeamData.UnderScoredGames * 0.50 + _headToHeadData.UnderScoredGames * 0.50;
        
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
             (_homeTeamData.UnderScoredGames >= goalThreshold ||
              _awayTeamData.UnderScoredGames >= goalThreshold)) ||
             (_homeTeamData.UnderScoredGames >= threshold && _awayTeamData.UnderScoredGames >= threshold)
            
            )
        {
            return (true, probability);
        }
        
        return (false, probability);
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


using AnalyseApp.Enums;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.models;

namespace AnalyseApp.Services;

public class DataService: IDataService
{
    private readonly List<Matches> _historicalMatches;

    public DataService(IFileProcessor fileProcessor)
    {
        _historicalMatches = fileProcessor.GetHistoricalMatchesBy();
    }
    
    public HeadToHeadData GetHeadToHeadDataBy(string homeTeam, string awayTeam, DateTime playedOn)
    {
        var matches = _historicalMatches
            .GetHeadToHeadMatchesBy(homeTeam, awayTeam, playedOn)
            .ToList();

        var homeTeamData = GetTeamGoalsDataBy(homeTeam, matches);
        var awayTeamData = GetTeamGoalsDataBy(awayTeam, matches);

        var overTwoGoals = GetOverGameAvg(matches) + 0.2;
        var underThreeGoals = GetUnderGameAvg(matches) + 0.3;
        var twoToThreeGoals = GetTwoToThreeGameAvg(matches);
        var goalGoal = GetBothScoredGameAvg(matches) + 0.1;
        var noGoals = GetZeroScoredGameAvg(matches);
        var overThreeGoals = GetMoreThanThreeGoalGameAvg(matches);
        var homeTeamWon = matches.GetGameAvgBy(
            matches.Count(i => i.HomeTeam == homeTeam),
            match => match.HomeTeam == homeTeam && match.FTHG > match.FTAG ||
                                    match.AwayTeam == homeTeam && match.FTHG < match.FTAG
        );
        var awayTeamWon = matches.GetGameAvgBy(
            matches.Count(i => i.AwayTeam == awayTeam),
            match => match.HomeTeam == awayTeam && match.FTHG > match.FTAG ||
                     match.AwayTeam == awayTeam && match.FTHG < match.FTAG
        );

        var headToHead = new HeadToHeadData(
            matches.Count,
            homeTeamData,
            awayTeamData,
            overTwoGoals, 
            underThreeGoals, 
            twoToThreeGoals,
            goalGoal,
            noGoals,
            overThreeGoals,
            homeTeamWon,
            awayTeamWon
        );

        headToHead = headToHead with { Suggestion = GetHighValue(headToHeadData: headToHead) };
        
        return headToHead;
    }

    /// <summary>
    /// Prepare Teams data
    /// </summary>
    /// <param name="teamName">Team name</param>
    /// <param name="historicalMatches">List of teams matches</param>
    /// <returns></returns>
    public TeamData GetTeamDataBy(string teamName, IList<Matches> historicalMatches)
    {
        var games = historicalMatches
            .Where(i => i.HomeTeam == teamName || i.AwayTeam == teamName)
            .Take(6)
            .ToList();
        
         var teamGoals = GetTeamGoalsDataBy(teamName, games);
        
        var currentLeagueGames = historicalMatches
            .GetCurrentLeagueGamesBy(teamName, 2023)
            .ToList();

        var teamSeasonGoals = GetTeamGoalsDataBy(teamName, currentLeagueGames);
        
        var teamResults = GetMatchResults(games);
        var teamOdds = GetMatchOdds(games, teamName);
        
        var lastThreeMatchResult = GetLastThreeMatchesBetType(games);
        var teamScoredGames = GetTeamScoredGamesAvg(games, teamName);
        var teamConcededGames = GetTeamAllowedGamesAvg(games, teamName);
        var totalWinAvg = GetWinGameAvg(games, teamName);

        var teamData = new TeamData(
            games.Count, 
            teamResults,
            teamOdds,
            teamGoals,
            teamSeasonGoals,
            teamScoredGames,
            teamConcededGames,
            lastThreeMatchResult
        );

        teamData = teamData with { Suggestion = GetHighValue(teamData: teamData) };
        
        return teamData;
    }

    private static TeamResult GetMatchResults(List<Matches> games)
    {
        var overTwoGoals = GetOverGameAvg(games);
        var underTwoGoals = GetUnderGameAvg(games);
        var twoToThreeGoals = GetTwoToThreeGameAvg(games);
        var bothScoredGoals = GetBothScoredGameAvg(games);
        var noGoal = GetZeroScoredGameAvg(games);
        var overThreeGoals = GetMoreThanThreeGoalGameAvg(games);

        var matchResult = new TeamResult(overTwoGoals, bothScoredGoals, twoToThreeGoals, underTwoGoals);
        return matchResult;
    } 
    
    private static TeamOdds GetMatchOdds(List<Matches> games, string teamName)
    {
        
        var homeSideWin = GetHomeWinGameAvg(games, teamName);
        var awaySideWin = GetAwayWinGameAvg(games, teamName);
        var draws = GetDrawGameAvg(games);

        var matchResult = new TeamOdds(homeSideWin, awaySideWin, draws);
        return matchResult;
    }

    /// <summary>
    /// Prepare goals data for team
    /// </summary>
    /// <param name="teamName">Team name</param>
    /// <param name="matches">List of matches</param>
    /// <returns></returns>
    private static TeamGoals GetTeamGoalsDataBy(string teamName, List<Matches> matches)
    {
        // Teams total scored Goals in home and away side
        var homeMatches = matches.Where(item => item.HomeTeam == teamName).ToList();
        var awayMatches = matches.Where(item => item.AwayTeam == teamName).ToList();
        
        var homeScoredGoals = homeMatches.Select(s => s.FTHG).Sum().GetValueOrDefault();
        var awayScoredGoals = awayMatches.Select(s => s.FTAG).Sum().GetValueOrDefault();

        // Teams total conceded Gaols in home and away side
        var homeConcededGoals = homeMatches.Select(s => s.FTAG).Sum().GetValueOrDefault();
        var awayConcededGoals = awayMatches.Select(s => s.FTHG).Sum().GetValueOrDefault();

        // Calculate Averages
        var homeScoredAvg = homeScoredGoals / (double)homeMatches.Count;
        var awayScoredAvg = awayScoredGoals / (double)awayMatches.Count;

        var homeConcededAvg = homeConcededGoals / (double)homeMatches.Count;
        var awayConcededAvg = awayConcededGoals / (double)awayMatches.Count;

        var teamTotalScores = homeScoredGoals + awayScoredGoals;
        var teamTotalConceded = homeConcededGoals + awayConcededGoals;

        var totalScoredAvg = teamTotalScores / (double)matches.Count;
        var totalConcededAvg = teamTotalConceded / (double)matches.Count;

        // Calculate probabilities
        var homeScoringPower = homeScoredAvg.GetScoredGoalProbabilityBy();
        var awayScoringPower = awayScoredAvg.GetScoredGoalProbabilityBy();
        var totalScoringPower = totalScoredAvg.GetScoredGoalProbabilityBy();
        var homeConcededPower = homeConcededAvg.GetScoredGoalProbabilityBy();
        var awayConcededPower = awayConcededAvg.GetScoredGoalProbabilityBy();
        var totalConcededPower = totalConcededAvg.GetScoredGoalProbabilityBy();
        
        var goalData = new TeamGoals(
            new Goals(matches.Count, teamTotalScores, teamTotalConceded, totalScoredAvg, totalConcededAvg, totalScoringPower, totalConcededPower),
            new Goals(homeMatches.Count, homeScoredGoals, homeConcededGoals, homeScoredAvg, homeConcededAvg, homeScoringPower, homeConcededPower),
            new Goals(awayMatches.Count, awayScoredGoals, awayConcededGoals, awayScoredAvg, awayConcededAvg, awayScoringPower, awayConcededPower)
        );
        return goalData;
    }

    private static BetType GetLastThreeMatchesBetType(IEnumerable<Matches> matches)
    {
        var lastThreeMatches = matches.Take(3).ToList();
        if (lastThreeMatches.All(item => item.FTHG + item.FTAG > 2))
        {
            return BetType.OverTwoGoals;
        }
        if (lastThreeMatches.All(item => item is { FTHG: > 0, FTAG: > 0 }))
        {
            return BetType.BothTeamScoreGoals;
        }
        if (lastThreeMatches.All(item => item.FTHG + item.FTAG is 2 or 3))
        {
            return BetType.TwoToThreeGoals;
        }
        return lastThreeMatches.All(item => item.FTHG + item.FTAG < 3) 
            ? BetType.UnderThreeGoals 
            : BetType.Unknown;
    }

    private static Suggestion GetHighValue(HeadToHeadData? headToHeadData = null, TeamData? teamData = null)
    {
        Dictionary<string, double> probabilityMap;

        if (teamData is not null)
        {
            probabilityMap = new Dictionary<string, double>
            {
                { "OverScoredGames", teamData.TeamResult.OverTwoGoals },
                { "BothTeamScoredGames", teamData.TeamResult.BothScoredGoals },
                { "TwoToThreeGoalsGames", teamData.TeamResult.TwoToThreeGoals },
                { "UnderScoredGames", teamData.TeamResult.UnderTwoGoals },
            };
        }
        else if (headToHeadData is not null)
        {
            probabilityMap = new Dictionary<string, double>
            {
                { "OverScoredGames", headToHeadData.OverScoredGames },
                { "BothTeamScoredGames", headToHeadData.BothTeamScoredGames },
                { "TwoToThreeGoalsGames", headToHeadData.TwoToThreeGoalsGames },
                { "UnderScoredGames", headToHeadData.UnderTwoScoredGames }
            };
        }
        else
        {
            throw new ArgumentException("Either HeadToHeadData or TeamData must be provided.");
        }

        var result = probabilityMap.MaxBy(i => i.Value);
        
        return new Suggestion(result.Key, result.Value);
    }

    private static double GetTeamScoredGooalsAvg(IReadOnlyCollection<Matches> matches, string teamName)
    {
        var goals = matches.Where(item => item.HomeTeam == teamName).Sum(s => s.FTHG) +
                            matches.Where(item => item.AwayTeam == teamName).Sum(s => s.FTAG);

        var avg = goals / (double)matches.Count;

        return avg.GetValueOrDefault();
    }
    
    private static double GetTeamScoredGamesAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(matches.Count, match => match.FTHG > 0 && match.HomeTeam == teamName ||
                                                     match.FTAG > 0 && match.AwayTeam == teamName);
    
    private static double GetTeamAllowedGamesAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(matches.Count, match => match.FTAG > 0 && match.HomeTeam == teamName ||
                                                     match.FTHG > 0 && match.AwayTeam == teamName);

    private static double GetTwoToThreeGameAvg(IReadOnlyCollection<Matches> matches) => 
        matches.GetGameAvgBy(
            matches.Count, 
            match => match.FTHG + match.FTAG == 3 || match.FTHG + match.FTAG == 2
        );

    private static double GetMoreThanThreeGoalGameAvg(IReadOnlyCollection<Matches> matches) => 
        matches.GetGameAvgBy(matches.Count, match => match.FTHG + match.FTAG > 3);
    
    private static double GetUnderGameAvg(IReadOnlyCollection<Matches> matches) => 
        matches.GetGameAvgBy(matches.Count, match => match.FTHG + match.FTAG < 2);

    private static double GetOverGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match.FTHG + match.FTAG > 2);
    
    private static double GetBothScoredGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match is { FTHG: > 0, FTAG: > 0 });
    
    private static double GetZeroScoredGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match is { FTHG: 0, FTAG: 0 });
    
    private static double GetHomeWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(matches.Count(i => i.HomeTeam == teamName), match => match.HomeTeam == teamName && match.FTHG > match.FTAG);
   
    private static double GetDrawGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match.FTHG == match.FTAG);

    private static double GetAwayWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(matches.Count(i => i.AwayTeam == teamName), match => match.AwayTeam == teamName && match.FTHG < match.FTAG);
    
    private static double GetWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(
            matches.Count, 
            match => (match.AwayTeam == teamName && match.FTHG < match.FTAG) ||
                                    match.HomeTeam == teamName && match.FTHG > match.FTAG
        );
}
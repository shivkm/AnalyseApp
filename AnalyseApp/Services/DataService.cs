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
    
    public HeadToHeadData GetHeadToHeadDataBy(string homeTeam, string awayTeam, string playedOn)
    {
        var matches = _historicalMatches
            .GetHeadToHeadMatchesBy(homeTeam, awayTeam, playedOn)
            .ToList();
        
        var scored = matches.Average(i => i.FTHG + i.FTAG);
        var overScoredAvg = GetOverGameAvg(matches);
        var underScoredAvg = GetUnderGameAvg(matches);
        var twoToThreeAvg = GetTwoToThreeGameAvg(matches);
        var bothTeamsScoredAvg = GetBothScoredGameAvg(matches);
        var zeroZeroGamesAvg = GetZeroScoredGameAvg(matches);
        var moreThanThreeGoalGameAvg = GetMoreThanThreeGoalGameAvg(matches);
        var homeWinAvg = matches.GetGameAvgBy(
            matches.Count,
            match => match.HomeTeam == homeTeam && match.FTHG > match.FTAG ||
                                    match.AwayTeam == homeTeam && match.FTHG < match.FTAG
        );
        var awayWinAvg = matches.GetGameAvgBy(
            matches.Count,
            match => match.HomeTeam == awayTeam && match.FTHG > match.FTAG ||
                                    match.AwayTeam == awayTeam && match.FTHG < match.FTAG
        );
        var scoreProbability = scored.GetValueOrDefault().GetScoredGoalProbabilityBy();
        
        var headToHead = new HeadToHeadData(
            matches.Count, 
            scoreProbability,
            overScoredAvg, 
            underScoredAvg, 
            twoToThreeAvg,
            bothTeamsScoredAvg,
            zeroZeroGamesAvg,
            moreThanThreeGoalGameAvg,
            homeWinAvg,
            awayWinAvg
        );
        headToHead = headToHead with { Suggestion = GetHighValue(headToHeadData: headToHead) };
        
        return headToHead;
    }

    public TeamData GetTeamDataBy(string teamName, IEnumerable<Matches> data)
    {
        var matches = data
                .Where(i => i.HomeTeam == teamName || i.AwayTeam == teamName)
                .Take(6)
                .ToList();
        
        var scored = matches.Count(item => 
            item.FTHG > 0 && item.HomeTeam == teamName ||
            item.FTAG > 0 && item.AwayTeam == teamName) / (double)matches.Count;
        
        var overScoredAvg = GetOverGameAvg(matches);
        var underScoredAvg = GetUnderGameAvg(matches);
        var twoToThreeAvg = GetTwoToThreeGameAvg(matches);
        var bothTeamsScoredAvg = GetBothScoredGameAvg(matches);
        var zeroZeroGamesAvg = GetZeroScoredGameAvg(matches);
        var homeWinAvg = GetHomeWinGameAvg(matches, teamName);
        var awayWinAvg = GetAwayWinGameAvg(matches, teamName);
        var winGameAvg = GetWinGameAvg(matches, teamName);
        var teamScoredGames = GetTeamScoredGamesAvg(matches, teamName);
        var teamAllowedGames = GetTeamAllowedGamesAvg(matches, teamName);
        var moreThanThreeGoalGameAvg = GetMoreThanThreeGoalGameAvg(matches);
        var lastThreeMatchResult = GetLastThreeMatchesBetType(matches);
        
        var scoreProbability = scored.GetScoredGoalProbabilityBy();
        
        var teamData = new TeamData(
            matches.Count, 
            scoreProbability,
            overScoredAvg, 
            underScoredAvg, 
            twoToThreeAvg,
            bothTeamsScoredAvg,
            zeroZeroGamesAvg,
            moreThanThreeGoalGameAvg,
            homeWinAvg,
            awayWinAvg,
            winGameAvg,
            teamScoredGames,
            teamAllowedGames,
            lastThreeMatchResult
        );

        teamData = teamData with { Suggestion = GetHighValue(teamData: teamData) };
        
        return teamData;
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
                { "OverScoredGames", teamData.OverScoredGames },
                { "BothTeamScoredGames", teamData.BothTeamScoredGames },
                { "TwoToThreeGoalsGames", teamData.TwoToThreeGoalsGames },
                { "UnderScoredGames", teamData.UnderScoredGames },
                { "ZeroZeroGames", teamData.ZeroZeroGoalGamesAvg }
            };
        }
        else if (headToHeadData is not null)
        {
            probabilityMap = new Dictionary<string, double>
            {
                { "OverScoredGames", headToHeadData.OverScoredGames },
                { "BothTeamScoredGames", headToHeadData.BothTeamScoredGames },
                { "TwoToThreeGoalsGames", headToHeadData.TwoToThreeGoalsGames },
                { "UnderScoredGames", headToHeadData.UnderScoredGames }
            };
        }
        else
        {
            throw new ArgumentException("Either HeadToHeadData or TeamData must be provided.");
        }

        var result = probabilityMap.MaxBy(i => i.Value);
        
        return new Suggestion(result.Key, result.Value);
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
        matches.GetGameAvgBy(matches.Count, match => match.FTHG + match.FTAG < 3);

    private static double GetOverGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match.FTHG + match.FTAG > 2);
    
    private static double GetBothScoredGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match is { FTHG: > 0, FTAG: > 0 });
    
    private static double GetZeroScoredGameAvg(IReadOnlyCollection<Matches> matches) =>
        matches.GetGameAvgBy(matches.Count, match => match is { FTHG: 0, FTAG: 0 });
    
    private static double GetHomeWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(matches.Count, match => match.HomeTeam == teamName && match.FTHG > match.FTAG);
    
    private static double GetAwayWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(matches.Count, match => match.AwayTeam == teamName && match.FTHG < match.FTAG);
    
    private static double GetWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(
            matches.Count, 
            match => (match.AwayTeam == teamName && match.FTHG < match.FTAG) ||
                                    match.HomeTeam == teamName && match.FTHG > match.FTAG
        );
}
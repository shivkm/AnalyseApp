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
        
        var scored = matches.Average(i => i.FTHG + i.FTAG);
        var overScoredAvg = GetOverGameAvg(matches);
        var underScoredAvg = GetUnderGameAvg(matches);
        var twoToThreeAvg = GetTwoToThreeGameAvg(matches);
        var bothTeamsScoredAvg = GetBothScoredGameAvg(matches);
        var zeroZeroGamesAvg = GetZeroScoredGameAvg(matches);
        var homeWinAvg = GetHomeWinGameAvg(matches, teamName);
        var awayWinAvg = GetAwayWinGameAvg(matches, teamName);
        var winGameAvg = GetWinGameAvg(matches, teamName);
        var scoreProbability = scored.GetValueOrDefault().GetScoredGoalProbabilityBy();
        
        var teamData = new TeamData(
            matches.Count, 
            scoreProbability,
            overScoredAvg, 
            underScoredAvg, 
            twoToThreeAvg,
            bothTeamsScoredAvg,
            zeroZeroGamesAvg,
            homeWinAvg,
            awayWinAvg,
            winGameAvg
        );

        teamData = teamData with { Suggestion = GetHighValue(teamData: teamData) };
        
        return teamData;
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
                { "ZeroZeroGames", teamData.ZeroZeroGames }
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

    private static double GetTwoToThreeGameAvg(IReadOnlyCollection<Matches> matches) => 
        matches.GetGameAvgBy(
            matches.Count, 
            match => match.FTHG + match.FTAG == 3 || match.FTHG + match.FTAG == 2
        );

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
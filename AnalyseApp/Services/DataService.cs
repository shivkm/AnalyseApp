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

        var homeTeamScoringAvg = GetTeamScoredGooalsAvg(matches,homeTeam);
        var awayTeamScoringAvg = GetTeamScoredGooalsAvg(matches, awayTeam);

        var overTwoGoals = GetOverGameAvg(matches) + 0.2;
        var underThreeGoals = GetUnderGameAvg(matches) + 0.3;
        var twoToThreeGoals = GetTwoToThreeGameAvg(matches);
        var goalGoal = GetBothScoredGameAvg(matches) + 0.1;
        var noGoals = GetZeroScoredGameAvg(matches);
        var overThreeGoals = GetMoreThanThreeGoalGameAvg(matches);
        var homeTeamWon = matches.GetGameAvgBy(
            matches.Count,
            match => match.HomeTeam == homeTeam && match.FTHG > match.FTAG ||
                                    match.AwayTeam == homeTeam && match.FTHG < match.FTAG
        );
        var awayTeamWon = matches.GetGameAvgBy(
            matches.Count,
            match => match.HomeTeam == awayTeam && match.FTHG > match.FTAG ||
                                    match.AwayTeam == awayTeam && match.FTHG < match.FTAG
        );
        var homeScoringPower = homeTeamScoringAvg.GetScoredGoalProbabilityBy();
        var awayScoringPower = awayTeamScoringAvg.GetScoredGoalProbabilityBy();

        var headToHead = new HeadToHeadData(
            matches.Count,
            homeScoringPower,
            awayScoringPower,
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

    public TeamData GetTeamDataBy(string teamName, IEnumerable<Matches> data)
    {
        var matches = data.Where(i => i.HomeTeam == teamName || i.AwayTeam == teamName)
                .Take(6)
                .ToList();
        // teams scored and conceded goals
        // home side scored and conceded gooals
        // away side scored and conceded goals
        // if home home side score more goals than away and conceded goals athome and  
        var homeScored = matches.Where(item => item.HomeTeam == teamName).Select(s => s.FTHG).Sum();
        var awayScored = matches.Where(item => item.AwayTeam == teamName).Select(s => s.FTAG).Sum();

        var homeConceded = matches.Where(item => item.HomeTeam == teamName).Select(s => s.FTAG).Sum();
        var awayConceded = matches.Where(item => item.AwayTeam == teamName).Select(s => s.FTHG).Sum();

        var homeScoredAvg = homeScored / (double)matches.Count;
        var awayScoredAvg = awayScored / (double)matches.Count;

        var homeConcededAvg = homeConceded / (double)matches.Count;
        var awayConcededAvg = awayConceded / (double)matches.Count;

        var totalScoredAvg = (homeScoredAvg + awayScoredAvg) / 2;
        var totalConcededAvg = (homeConcededAvg + awayConcededAvg) / 2;

        var teamScores = homeScored + awayScored;
        var teamConceded = homeConceded + awayConceded;

        var overTwoGoals = GetOverGameAvg(matches) + 0.2;
        var underTwoGoals = GetUnderGameAvg(matches);
        var twoToThreeGoals = GetTwoToThreeGameAvg(matches) + 0.3;
        var goalGoals = GetBothScoredGameAvg(matches) +0.1;
        var noGoal = GetZeroScoredGameAvg(matches);
        var homeSideWin = GetHomeWinGameAvg(matches, teamName) + 0.5;
        var awaySideWin = GetAwayWinGameAvg(matches, teamName);
        var totalWinAvg = GetWinGameAvg(matches, teamName);
        var teamScoredGames = GetTeamScoredGamesAvg(matches, teamName);
        var teamConcededGames = GetTeamAllowedGamesAvg(matches, teamName);
        var overThreeGoals = GetMoreThanThreeGoalGameAvg(matches);
        var lastThreeMatchResult = GetLastThreeMatchesBetType(matches);

        var homeScoringPower = homeScoredAvg?.GetScoredGoalProbabilityBy();
        var awayScoringPower = awayScoredAvg?.GetScoredGoalProbabilityBy();
        var homeConcededPower = homeConcededAvg?.GetScoredGoalProbabilityBy();
        var awayConcededPower = awayConcededAvg?.GetScoredGoalProbabilityBy();

        var scoringPower = totalScoredAvg?.GetScoredGoalProbabilityBy();
        var concededPower = totalConcededAvg?.GetScoredGoalProbabilityBy();
        
        var teamData = new TeamData(
            new Goals(
                teamScores.GetValueOrDefault(),
                teamConceded.GetValueOrDefault(),
                homeScored.GetValueOrDefault(),
                homeConceded.GetValueOrDefault(),
                awayScored.GetValueOrDefault(),
                awayConceded.GetValueOrDefault()
            ),
            matches.Count, 
            scoringPower,
            concededPower,
            homeScoringPower,
            homeConcededPower,
            awayScoringPower,
            awayConcededPower,
            overTwoGoals, 
            underTwoGoals, 
            twoToThreeGoals,
            goalGoals,
            noGoal,
            overThreeGoals,
            homeSideWin,
            awaySideWin,
            totalWinAvg,
            teamScoredGames,
            teamConcededGames,
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
                { "UnderScoredGames", teamData.UnderTwoScoredGames },
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
        matches.GetGameAvgBy(matches.Count, match => match.HomeTeam == teamName && match.FTHG > match.FTAG);
    
    private static double GetAwayWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(matches.Count, match => match.AwayTeam == teamName && match.FTHG < match.FTAG);
    
    private static double GetWinGameAvg(IReadOnlyCollection<Matches> matches, string teamName) =>
        matches.GetGameAvgBy(
            matches.Count, 
            match => (match.AwayTeam == teamName && match.FTHG < match.FTAG) ||
                                    match.HomeTeam == teamName && match.FTHG > match.FTAG
        );

    public TeamData GetTeamSeasonBy(string teamName, IEnumerable<Matches> data)
    {
        throw new NotImplementedException();
    }
}
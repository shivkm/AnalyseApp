using System.Text.Json;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.Models;
using Microsoft.Data.Sqlite;

namespace AnalyseApp.Services;

public class DataProcessor: IDataProcessor
{
    private const string connectionString = "Data Source=matchdata.db";

    public List<MatchAverage> CalculateMatchAveragesDataBy(IEnumerable<Match> historicalMatches, DateTime upcomingMatchDate)
    {
        // if (IsDataUpToDate(upcomingMatchDate, connectionString))
        // {
        //     return LoadComputedDataFromDatabase(connectionString);
        // }
        var matchAverages = new List<MatchAverage>();

        foreach (var historicalMatch in historicalMatches)
        {
            var homeTeam = historicalMatch.HomeTeam;
            var awayTeam = historicalMatch.AwayTeam;
            var datTime = historicalMatch.Date.Parse();
            var homeScore = historicalMatch.FullTimeHomeGoals;
            var awayScore = historicalMatch.FullTimeAwayGoals;
            
            var matchAverage = CalculateGoalMatchAverageBy(historicalMatches, homeTeam, awayTeam, datTime, homeScore, awayScore);
            matchAverages.Add(matchAverage);
        }
        
        // Save the newly computed data to the database
        CreateTableInTheDatabase(connectionString, """
                                                    
                                                        CREATE TABLE IF NOT EXISTS MatchAverage (
                                                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                            Data TEXT
                                                        );
                                                        
                                                    """);
        SaveComputedDataToDatabase(matchAverages, connectionString);
        return matchAverages;
    }
 
    public TeamData CalculateTeamData(IEnumerable<Match> matches, string teamName)
    {
        var teamMatches = matches.Where(m => m.HomeTeam == teamName || m.AwayTeam == teamName);
        if (teamMatches.Count() < 3)return new TeamData();
        
        var homeMatches = teamMatches.Where(m => m.HomeTeam == teamName);
        var awayMatches = teamMatches.Where(m => m.AwayTeam == teamName);

        var homeScoredGoalsAverage = homeMatches.Average(m => m.FullTimeHomeGoals);
        var homeConcededGoalsAverage = homeMatches.Average(m => m.FullTimeAwayGoals);
        var awayScoredGoalsAverage = awayMatches.Average(m => m.FullTimeAwayGoals);
        var awayConcededGoalsAverage = awayMatches.Average(m => m.FullTimeHomeGoals);
        
        var homeHalfTimeScoredGoalsAverage = homeMatches.Average(m => m.HalfTimeHomeGoals);
        var homeHalfTimeConcededGoalsAverage = homeMatches.Average(m => m.HalfTimeAwayGoals);
        var awayHalfTimeScoredGoalsAverage = awayMatches.Average(m => m.HalfTimeAwayGoals);
        var awayHalfTimeConcededGoalsAverage = awayMatches.Average(m => m.HalfTimeHomeGoals);

        var homeZeroZeroMatchAverage = homeMatches.GetAverageBy(zeroZeroGoals: true);
        var awayZeroZeroMatchAverage = awayMatches.GetAverageBy(zeroZeroGoals: true);
        var homeUnderThreeGoalsMatchAverage = homeMatches.GetAverageBy();
        var awayUnderThreeGoalsMatchAverage = awayMatches.GetAverageBy();
        var homeOverTwoGoalsMatchAverage = homeMatches.GetAverageBy(overTwoGoals: true);
        var awayOverTwoGoalsMatchAverage = awayMatches.GetAverageBy(overTwoGoals: true);
        var homeGoalGoalsMatchAverage = homeMatches.GetAverageBy(goalGoal: true);
        var awayGoalGoalsMatchAverage = awayMatches.GetAverageBy(goalGoal: true);
        var homeTwoToThreeGoalsMatchAverage = homeMatches.GetAverageBy(twoToThreeGoals: true);
        var awayTwoToThreeGoalsMatchAverage = awayMatches.GetAverageBy(twoToThreeGoals: true);

        float  homeScoredGoalsWeighted = 0,
               awayScoredGoalsWeighted = 0,
               homeConcededGoalsWeighted = 0,
               awayConcededGoalsWeighted = 0,
               homeHalfTimeScoredGoalsWeighted = 0,
               awayHalfTimeScoredGoalsWeighted = 0,
               homeHalfTimeConcededGoalsWeighted = 0,
               awayHalfTimeConcededGoalsWeighted = 0;
        
        float totalWeight = 0;

        foreach (var match in teamMatches)
        {
            var weight = CalculateTimeDecayWeight(match.Date.Parse());
            totalWeight += weight;

            if (match.HomeTeam == teamName)
            {
                homeScoredGoalsWeighted += weight * match.FullTimeHomeGoals;
                homeConcededGoalsWeighted += weight * match.FullTimeAwayGoals;
                homeHalfTimeScoredGoalsWeighted += weight * match.HalfTimeHomeGoals;
                homeHalfTimeConcededGoalsWeighted += weight * match.HalfTimeAwayGoals;
            }
            if (match.AwayTeam == teamName)
            {
                awayScoredGoalsWeighted += weight * match.FullTimeAwayGoals;
                awayConcededGoalsWeighted += weight * match.FullTimeHomeGoals;
                awayHalfTimeScoredGoalsWeighted += weight * match.HalfTimeAwayGoals;
                awayHalfTimeConcededGoalsWeighted += weight * match.HalfTimeHomeGoals;
            }
        }
        
        var weightedHomeGoals = totalWeight > 0 ? homeScoredGoalsWeighted / totalWeight : homeScoredGoalsAverage;
        var weightedAwayGoals = totalWeight > 0 ? awayScoredGoalsWeighted / totalWeight : awayScoredGoalsAverage;
        var weightedHomeConcededGoals = totalWeight > 0 ? homeConcededGoalsWeighted / totalWeight : homeConcededGoalsAverage;
        var weightedAwayConcededGoals = totalWeight > 0 ? awayConcededGoalsWeighted / totalWeight : awayConcededGoalsAverage;
        var weightedHomeHalfTimeGoals = totalWeight > 0 ? homeHalfTimeScoredGoalsWeighted / totalWeight : homeHalfTimeScoredGoalsAverage;
        var weightedAwayHalfTimeGoals = totalWeight > 0 ? awayHalfTimeScoredGoalsWeighted / totalWeight : awayHalfTimeScoredGoalsAverage;
        var weightedHomeHalfTimeConcededGoals = totalWeight > 0 ? homeHalfTimeConcededGoalsWeighted / totalWeight : homeHalfTimeConcededGoalsAverage;
        var weightedAwayHalfTimeConcededGoals = totalWeight > 0 ? awayHalfTimeConcededGoalsWeighted / totalWeight : awayHalfTimeConcededGoalsAverage;


        return new TeamData
        {
            TeamName = teamName,
            ScoredGoalsAverage = (weightedHomeGoals + weightedAwayGoals) / 2,
            ConcededGoalsAverage = (weightedHomeConcededGoals + weightedAwayConcededGoals) / 2,
            HalfTimeScoredGoalAverage = (weightedHomeHalfTimeGoals + weightedAwayHalfTimeGoals) / 2,
            HalfTimeConcededGoalAverage = (weightedHomeHalfTimeConcededGoals + weightedAwayHalfTimeConcededGoals) / 2,
            ZeroZeroMatchAverage = (homeZeroZeroMatchAverage + awayZeroZeroMatchAverage) / 2,
            UnderThreeGoalsMatchAverage = (homeUnderThreeGoalsMatchAverage + awayUnderThreeGoalsMatchAverage) / 2,
            OverTwoGoalsMatchAverage = (homeOverTwoGoalsMatchAverage + awayOverTwoGoalsMatchAverage) / 2,
            GoalGoalsMatchAverage = (homeGoalGoalsMatchAverage + awayGoalGoalsMatchAverage) / 2,
            TwoToThreeMatchAverage = (homeTwoToThreeGoalsMatchAverage + awayTwoToThreeGoalsMatchAverage) / 2,
        };
    }

    public MatchData GetLastSixMatchDataBy(List<Match> historicalMatches, string home, string away, DateTime playedOn)
    {
        var homeData = GetScoredGoalMatchCountBy(historicalMatches, home, playedOn, true);
        var awayData = GetScoredGoalMatchCountBy(historicalMatches, away, playedOn);
        var headToHead = GetHeadToHeadDataBy(historicalMatches, home, away, playedOn);

        var matchData = new MatchData
        {
            HomeTeam = homeData,
            AwayTeam = awayData,
            HeadToHeadData = headToHead
        };

        return matchData;
    }

    private static TeamData GetScoredGoalMatchCountBy(List<Match> historicalMatches, string team, DateTime playedOn, bool isHome = false)
    {
        var matches = historicalMatches
            .GetMatchBy(match => match.Date.Parse() < playedOn &&
                                 (match.HomeTeam == team || match.AwayTeam == team))
            .Take(6)
            .ToList();

        var scoredGoalsMatchCount = matches.Count(i =>
            i.HomeTeam == team && i.FullTimeHomeGoals > 0 ||
            i.AwayTeam == team && i.FullTimeAwayGoals > 0
        );
        
        var fieldScoredGoalsMatchCount = matches.Count(i =>
            isHome ? i.HomeTeam == team && i.FullTimeHomeGoals > 0 
                    : i.AwayTeam == team && i.FullTimeAwayGoals > 0
        );

        var matchGoals = new List<int>();
        
        foreach (var match in matches)
        {
            if (match.HomeTeam == team)
            {
                matchGoals.Add(Convert.ToInt32(match.FullTimeHomeGoals));
            }

            if (match.AwayTeam == team)
            {
                matchGoals.Add(Convert.ToInt32(match.FullTimeAwayGoals));
            }
        }

        matchGoals.Reverse();
        
        var lastMatchNotScored = false;
        var lastAndThirdLastScored = false;
        var lastAndSecondLastScored = false;
        
        if (matchGoals.Count >= 2)
        {
            lastMatchNotScored = matchGoals[0] == 0;
            lastAndThirdLastScored = matchGoals[0] > 0 && matchGoals[2] > 0;
            lastAndSecondLastScored = matchGoals[0] == 0 && matchGoals[1] == 0;
        }
       
        var teamData = new TeamData
        {
            TeamName = team,
            MatchCount = matches.Count,
            FieldMatchCount = matches.Count(i => isHome ? i.HomeTeam == team : i.AwayTeam == team),
            Output = "Match Goals: [" + string.Join(", ", matchGoals) + "]",
            LastMatchNotScored = lastMatchNotScored,
            LastAndThirdLastScored = lastAndThirdLastScored,
            LastTwoScored = lastAndSecondLastScored,
        };

        return teamData;
    }

    private static HeadToHeadData GetHeadToHeadDataBy(List<Match> historicalMatches, string homeTeam, string awayTeam,
        DateTime playedOn)
    {
        var matches = historicalMatches
            .GetMatchBy(match => match.Date.Parse() < playedOn &&
                                 (match.HomeTeam == homeTeam ||
                                  match.AwayTeam == homeTeam || 
                                  match.HomeTeam == awayTeam ||
                                  match.AwayTeam == awayTeam))
            .Take(6)
            .ToList();
        
        var lastThreeHomeMatchGoals = new List<int>();
        var lastThreeAwayMatchGoals = new List<int>();
        
        foreach (var match in matches.Take(3))
        {
            if (match.HomeTeam == homeTeam ||match.AwayTeam == homeTeam)
            {
                lastThreeHomeMatchGoals.Add(Convert.ToInt32(match.FullTimeHomeGoals));
            }

            if (match.HomeTeam == awayTeam ||match.AwayTeam == awayTeam)
            {
                lastThreeAwayMatchGoals.Add(Convert.ToInt32(match.FullTimeAwayGoals));
            }
        }

        var lastMatchNotScored = false;
        var lastAndThirdLastScored = false;
        var lastAndSecondLastScored = false;
        
        if (lastThreeHomeMatchGoals.Count >= 2 && lastThreeAwayMatchGoals.Count >= 2)
        {
            
            lastMatchNotScored = lastThreeHomeMatchGoals[0] == 0 || lastThreeAwayMatchGoals[0] == 0;
            lastAndThirdLastScored = lastThreeHomeMatchGoals[0] > 0 && lastThreeHomeMatchGoals[2] > 0 ||
                                         lastThreeAwayMatchGoals[0] > 0 && lastThreeAwayMatchGoals[2] > 0;
        
            lastAndSecondLastScored = lastThreeHomeMatchGoals[0] == 0 && lastThreeHomeMatchGoals[1] == 0 ||
                                          lastThreeAwayMatchGoals[0] == 0 && lastThreeAwayMatchGoals[1] == 0;
        }
       
        
        var headToHead = new HeadToHeadData(
            matches.Take(3).Count(),
            lastThreeHomeMatchGoals.All(m => m > 0),
            lastThreeAwayMatchGoals.All(m => m > 0),
            lastMatchNotScored,
            lastAndThirdLastScored,
            lastAndSecondLastScored
            );

        return headToHead;
    }
    
    public MatchAverage CalculateGoalMatchAverageBy(
        IEnumerable<Match> historicalMatches, 
        string homeTeam, string awayTeam, DateTime playedOn, float homeScore, float awayScore, bool currentLeague = false)
    {
        var goalAverage = currentLeague 
            ? GetCurrentLeagueGoalAverageBy(historicalMatches, homeTeam, awayTeam)
            : GetGoalAverageBy(historicalMatches, homeTeam, awayTeam);
        
        var homeAttackStrength = goalAverage.HomeScoredGoalAverage / goalAverage.LeagueHomeGoalAverage;
        var awayAttackStrength = goalAverage.AwayScoredGoalAverage / goalAverage.LeagueAwayGoalAverage;
        var homeDefenceStrength = goalAverage.HomeConcededGoalAverage / goalAverage.LeagueAwayGoalAverage;
        var awayDefenceStrength = goalAverage.AwayConcededGoalAverage / goalAverage.LeagueHomeGoalAverage;

        var over = homeScore + awayScore > 2;
        var goalGoal = homeScore > 0 && awayScore > 0;
        var matchAverage = new MatchAverage(
            $"{homeTeam}:{awayTeam}",
            (float)(homeAttackStrength * awayDefenceStrength * goalAverage.LeagueHomeGoalAverage),
            (float)(awayAttackStrength * homeDefenceStrength * goalAverage.LeagueAwayGoalAverage),
            playedOn
            )
        {
            OverUnder = over,
            GoalGoal = goalGoal
            
        };

        return matchAverage;
    }

    private static GoalAverages GetGoalAverageBy(IEnumerable<Match> historicalMatches, string homeTeam, string awayTeam)
    {
        var league = GetLeagueOfTeam(historicalMatches, homeTeam);
        var leagueGames = historicalMatches.GetMatchBy(league);

        var leagueHomeGoalAverage = CalculateAverageGoals(leagueGames, isHomeTeam: true);
        var leagueAwayGoalAverage = CalculateAverageGoals(leagueGames);

        var homeScoredGoalAverage = CalculateTeamAverageGoals(leagueGames, homeTeam, isHomeTeam: true);
        var homeConcededGoalAverage = CalculateTeamAverageGoals(leagueGames, homeTeam);

        var awayScoredGoalAverage = CalculateTeamAverageGoals(leagueGames, awayTeam);
        var awayConcededGoalAverage = CalculateTeamAverageGoals(leagueGames, awayTeam, isHomeTeam: true);

        
        return new GoalAverages(
            leagueHomeGoalAverage,
            leagueAwayGoalAverage,
            homeScoredGoalAverage,
            homeConcededGoalAverage,
            awayScoredGoalAverage,
            awayConcededGoalAverage
        );
    }
    
    private static GoalAverages GetCurrentLeagueGoalAverageBy(IEnumerable<Match> historicalMatches, string homeTeam, string awayTeam)
    {
        var league = GetLeagueOfTeam(historicalMatches, homeTeam);
        var leagueGames = historicalMatches.GetMatchBy(league, 2023);

        var leagueHomeGoalAverage = CalculateAverageGoals(leagueGames, isHomeTeam: true);
        var leagueAwayGoalAverage = CalculateAverageGoals(leagueGames);

        var homeScoredGoalAverage = CalculateTeamAverageGoals(leagueGames, homeTeam, isHomeTeam: true);
        var homeConcededGoalAverage = CalculateTeamAverageGoals(leagueGames, homeTeam);

        var awayScoredGoalAverage = CalculateTeamAverageGoals(leagueGames, awayTeam);
        var awayConcededGoalAverage = CalculateTeamAverageGoals(leagueGames, awayTeam, isHomeTeam: true);

        return new GoalAverages(
            leagueHomeGoalAverage,
            leagueAwayGoalAverage,
            homeScoredGoalAverage,
            homeConcededGoalAverage,
            awayScoredGoalAverage,
            awayConcededGoalAverage
        );
    }

    private static string GetLeagueOfTeam(IEnumerable<Match> matches, string teamName) => 
        matches.First(m => m.HomeTeam == teamName || m.AwayTeam == teamName).League;

    private static double CalculateAverageGoals(IEnumerable<Match> matches, bool isHomeTeam = false)
    {
        var goalCount = isHomeTeam 
            ? matches.Sum(m => m.FullTimeHomeGoals) 
            : matches.Sum(m => m.FullTimeAwayGoals);
        
        var matchCount = matches.Count();
        
        return matchCount > 0 ? (double)goalCount / matchCount : 0;
    }

    private static double CalculateTeamAverageGoals(IEnumerable<Match> matches, string teamName, bool isHomeTeam = false)
    {
        var teamMatches = isHomeTeam 
            ? matches.Where(m => m.HomeTeam == teamName) 
            : matches.Where(m => m.AwayTeam == teamName);
        
        var goalCount = isHomeTeam 
            ? teamMatches.Sum(m => m.FullTimeHomeGoals) 
            : teamMatches.Sum(m => m.FullTimeAwayGoals);
        
        var matchCount = teamMatches.Count();
        return matchCount > 0 ? (double)goalCount / matchCount : 0;
    }

    
    private Dictionary<string, TeamData> PrecomputeTeamData(IEnumerable<Match> matches)
    {
        var teamDataDictionary = new Dictionary<string, TeamData>();

        foreach (var teamName in matches.SelectMany(m => new[] { m.HomeTeam, m.AwayTeam }).Distinct())
        {
            teamDataDictionary[teamName] = CalculateTeamData(matches, teamName);
        }

        return teamDataDictionary;
    }


    private static float CalculateTimeDecayWeight(DateTime matchDate, float decayRate = 0.05f)
    {
        var daysSinceMatch = (DateTime.Now - matchDate).TotalDays;
        return (float)Math.Exp(-decayRate * daysSinceMatch);
    }

    private static bool IsDataUpToDate(DateTime date, string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = new SqliteCommand("SELECT Data FROM MatchAverage", connection);
        var result = command.ExecuteReader();

        if (result.Read())
        {
            var jsonData = result.GetString(0);
            var matchData = JsonSerializer.Deserialize<List<MatchData>>(jsonData);
            matchData = matchData.OrderByDescending(i => i.Date).ToList();
            return matchData.First().Date >= date;
        }

        return false;
    }
    
    private static List<MatchAverage> LoadComputedDataFromDatabase(string connectionString)
    {
        var matchDataList = new List<MatchAverage>();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = new SqliteCommand("SELECT Data FROM MatchAverage", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var jsonData = reader.GetString(0);
            var matchData = JsonSerializer.Deserialize<List<MatchAverage>>(jsonData);
            if (matchData != null)
            {
                matchDataList.AddRange(matchData);
            }
        }

        return matchDataList;
    }

    private static void SaveComputedDataToDatabase(List<MatchAverage> matchAverages, string connectionString)
    {
        var jsonData = JsonSerializer.Serialize(matchAverages);
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO MatchAverage (Data) VALUES (@Data)";
        command.Parameters.AddWithValue("@Data", jsonData);
        command.ExecuteNonQuery();
    }
    
    private static void CreateTableInTheDatabase(string connectionString, string commandText)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }
}
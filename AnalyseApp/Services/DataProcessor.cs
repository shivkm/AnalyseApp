using System.Text.Json;
using AnalyseApp.Extensions;
using AnalyseApp.Interfaces;
using AnalyseApp.Models;
using Microsoft.Data.Sqlite;

namespace AnalyseApp.Services;

public class DataProcessor: IDataProcessor
{
    private const string connectionString = "Data Source=matchdata.db";

    public List<MatchData> CalculateMatchAveragesDataBy(IEnumerable<Match> historicalMatches, DateTime upcomingMatchDate)
    {
        if (IsDataUpToDate(upcomingMatchDate, connectionString))
        {
            return LoadComputedDataFromDatabase(connectionString);
        }
        var precomputedTeamData = PrecomputeTeamData(historicalMatches);
        var matchDataList = new List<MatchData>();


        foreach (var historicalMatch in historicalMatches)
        {
            var homeTeamData = precomputedTeamData[historicalMatch.HomeTeam];
            var awayTeamData = precomputedTeamData[historicalMatch.AwayTeam];

            var matchData = homeTeamData.GetMatchDataBy(awayTeamData, historicalMatch.Date.Parse());

            matchData = matchData with
            {
                HomeWin = historicalMatch.FullTimeHomeGoals > historicalMatch.FullTimeAwayGoals,
                AwayWin = historicalMatch.FullTimeAwayGoals > historicalMatch.FullTimeHomeGoals,
                OverUnderTwoGoals = historicalMatch.FullTimeHomeGoals + historicalMatch.FullTimeAwayGoals > 2.5,
                BothTeamsScored = historicalMatch is { FullTimeHomeGoals: > 0, FullTimeAwayGoals: > 0 },
                TwoToThreeGoals =
                Math.Abs(historicalMatch.FullTimeHomeGoals + historicalMatch.FullTimeAwayGoals - 2.0) < 0.5 ||
                Math.Abs(historicalMatch.FullTimeHomeGoals + historicalMatch.FullTimeAwayGoals - 3.0) < 0.5
            };
            
            matchDataList.Add(matchData);
        }
        
        // Save the newly computed data to the database
        CreateTableInTheDatabase(connectionString);
        SaveComputedDataToDatabase(matchDataList, connectionString);
        return matchDataList;
    }
    
    public TeamData CalculateTeamData(IEnumerable<Match> matches, string teamName)
    {
        var teamMatches = matches.Where(m => m.HomeTeam == teamName || m.AwayTeam == teamName);

        if (teamMatches.Count() < 3)
        {
            return new TeamData();
        }
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
            GoalGoalsMatchAverage = (homeGoalGoalsMatchAverage + awayGoalGoalsMatchAverage) / 2
        };
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

        using var command = new SqliteCommand("SELECT Data FROM MatchDataJSON", connection);
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
    
    private static List<MatchData> LoadComputedDataFromDatabase(string connectionString)
    {
        var matchDataList = new List<MatchData>();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = new SqliteCommand("SELECT Data FROM MatchDataJSON", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var jsonData = reader.GetString(0);
            var matchData = JsonSerializer.Deserialize<List<MatchData>>(jsonData);
            if (matchData != null)
            {
                matchDataList.AddRange(matchData);
            }
        }

        return matchDataList;
    }

    private static void SaveComputedDataToDatabase(List<MatchData> matchData, string connectionString)
    {
        var jsonData = JsonSerializer.Serialize(matchData);
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO MatchDataJSON (Data) VALUES (@Data)";
        command.Parameters.AddWithValue("@Data", jsonData);
        command.ExecuteNonQuery();
    }
    
    private static void CreateTableInTheDatabase(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = 
            """
            
                CREATE TABLE IF NOT EXISTS MatchDataJSON (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Data TEXT
                );
                
            """;
        command.ExecuteNonQuery();
    }
}
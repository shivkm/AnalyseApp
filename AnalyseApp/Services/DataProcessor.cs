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
        float homeGoalsAverage = 0;
        float awayGoalsAverage = 0;
        float homeHalfTimeGoalsAverage = 0;
        float awayHalfTimeGoalsAverage = 0;
        float homeShortAverage = 0;
        float awayShortAverage = 0;
        float homeTargetShotsAverage = 0;
        float awayTargetShotsAverage = 0;
        
        float totalWeight = 0;
        
        foreach (var match in matches.Where(item => item.HomeTeam == teamName || item.AwayTeam == teamName))
        {
            var homeTeam = match.HomeTeam;
            var awayTeam = match.AwayTeam;
            var weight = CalculateTimeDecayWeight(match.Date.Parse());
            totalWeight += weight;
            if (match.HomeTeam == teamName)
            {
                homeGoalsAverage += weight * match.FullTimeHomeGoals / matches.GetGoalAverageRate(teamName);
                homeHalfTimeGoalsAverage += weight * match.HalfTimeHomeGoals / matches.GetGoalAverageRate(homeTeam, true);
                homeShortAverage += weight * match.HalfTimeHomeGoals / matches.GetShotAverageRate(homeTeam);
                homeTargetShotsAverage += weight * match.HalfTimeHomeGoals / matches.GetShotAverageRate(homeTeam, true);
            }
            if (match.AwayTeam == teamName)
            {
                awayGoalsAverage += weight * match.FullTimeAwayGoals / matches.GetGoalAverageRate(teamName);
                awayHalfTimeGoalsAverage += weight * match.HalfTimeAwayGoals / matches.GetGoalAverageRate(awayTeam, true);
                awayShortAverage += weight * match.HalfTimeAwayGoals / matches.GetShotAverageRate(awayTeam);
                awayTargetShotsAverage += weight * match.HalfTimeAwayGoals / matches.GetShotAverageRate(awayTeam, true);
            }
        }
        
        homeGoalsAverage = totalWeight > 0 ? homeGoalsAverage / totalWeight : 0;
        homeHalfTimeGoalsAverage = totalWeight > 0 ? homeHalfTimeGoalsAverage / totalWeight : 0;
        homeShortAverage = totalWeight > 0 ? homeShortAverage / totalWeight : 0;
        homeTargetShotsAverage = totalWeight > 0 ? homeTargetShotsAverage / totalWeight : 0;
        
        awayGoalsAverage = totalWeight > 0 ? awayGoalsAverage / totalWeight : 0;
        awayHalfTimeGoalsAverage = totalWeight > 0 ? awayHalfTimeGoalsAverage / totalWeight : 0;
        awayShortAverage = totalWeight > 0 ? awayShortAverage / totalWeight : 0;
        awayTargetShotsAverage = totalWeight > 0 ? awayTargetShotsAverage / totalWeight : 0;

        
        return new TeamData
        {
            TeamName = teamName,
            ScoredGoalsAverage = homeGoalsAverage,
            ConcededGoalsAverage = awayGoalsAverage,
            HalfTimeScoredGoalAverage = homeHalfTimeGoalsAverage,
            HalfTimeConcededGoalAverage = awayHalfTimeGoalsAverage,
            ScoredShotsAverage = homeShortAverage,
            ConcededShotsAverage = awayShortAverage,
            ScoredTargetShotsAverage = homeTargetShotsAverage,
            ConcededTargetShotsAverage = awayTargetShotsAverage
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
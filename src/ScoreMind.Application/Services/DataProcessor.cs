using System.Text.RegularExpressions;
using AnalyseApp.Models;
using ScoreMind.Application.Extensions;
using ScoreMind.Interfaces;

namespace ScoreMind.Application.Services;
//
// public class DataProcessor: IDataProcessor
// {
//     public TeamData CalculateTeamPerformanceBy(IEnumerable<Game> historicalGames, string teamName)
//     {
//         var teamHomeHistoricalGames = historicalGames.GetHistoricalGamesBy(teamName, true);
//         var teamAwayHistoricalGames = historicalGames.GetHistoricalGamesBy(teamName);
//
//       
//
//     }
//
//     
//     public TeamData CalculateTeamData(IEnumerable<Match> matches, string teamName)
//     {
//         float homeGoalsAverage = 0;
//         float awayGoalsAverage = 0;
//         float homeHalfTimeGoalsAverage = 0;
//         float awayHalfTimeGoalsAverage = 0;
//         float homeShortAverage = 0;
//         float awayShortAverage = 0;
//         float homeTargetShotsAverage = 0;
//         float awayTargetShotsAverage = 0;
//         
//         float totalWeight = 0;
//         
//         foreach (var match in matches.Where(item => item.HomeTeam == teamName || item.AwayTeam == teamName))
//         {
//             var homeTeam = match.HomeTeam;
//             var awayTeam = match.AwayTeam;
//             var weight = CalculateTimeDecayWeight(match.Date.Parse());
//             totalWeight += weight;
//             if (match.HomeTeam == teamName)
//             {
//                 homeGoalsAverage += weight * match.FullTimeHomeGoals / matches.GetGoalAverageRate(teamName);
//                 homeHalfTimeGoalsAverage += weight * match.HalfTimeHomeGoals / matches.GetGoalAverageRate(homeTeam, true);
//                 homeShortAverage +=
//                     weight * match.HalfTimeHomeGoals / matches.GetShotAverageRate(homeTeam);
//                 homeTargetShotsAverage +=
//                     weight * match.HalfTimeHomeGoals / matches.GetShotAverageRate(homeTeam, true);
//             }
//             if (match.AwayTeam == teamName)
//             {
//                 awayGoalsAverage += weight * match.FullTimeAwayGoals / matches.GetGoalAverageRate(teamName);
//                 awayHalfTimeGoalsAverage += weight * match.HalfTimeAwayGoals / matches.GetGoalAverageRate(awayTeam, true);
//                 awayShortAverage +=
//                     weight * match.HalfTimeAwayGoals / matches.GetShotAverageRate(awayTeam);
//             
//                 awayTargetShotsAverage +=
//                     weight * match.HalfTimeAwayGoals / matches.GetShotAverageRate(awayTeam, true);
//             }
//         }
//         
//         homeGoalsAverage = totalWeight > 0 ? homeGoalsAverage / totalWeight : 0;
//         homeHalfTimeGoalsAverage = totalWeight > 0 ? homeHalfTimeGoalsAverage / totalWeight : 0;
//         homeShortAverage = totalWeight > 0 ? homeShortAverage / totalWeight : 0;
//         homeTargetShotsAverage = totalWeight > 0 ? homeTargetShotsAverage / totalWeight : 0;
//         
//         awayGoalsAverage = totalWeight > 0 ? awayGoalsAverage / totalWeight : 0;
//         awayHalfTimeGoalsAverage = totalWeight > 0 ? awayHalfTimeGoalsAverage / totalWeight : 0;
//         awayShortAverage = totalWeight > 0 ? awayShortAverage / totalWeight : 0;
//         awayTargetShotsAverage = totalWeight > 0 ? awayTargetShotsAverage / totalWeight : 0;
//
//         
//         return new TeamData
//         {
//             TeamName = teamName,
//             ScoredGoalsAverage = homeGoalsAverage,
//             ConcededGoalsAverage = awayGoalsAverage,
//             HalfTimeScoredGoalAverage = homeHalfTimeGoalsAverage,
//             HalfTimeConcededGoalAverage = awayHalfTimeGoalsAverage,
//             ScoredShotsAverage = homeShortAverage,
//             ConcededShotsAverage = awayShortAverage,
//             ScoredTargetShotsAverage = homeTargetShotsAverage,
//             ConcededTargetShotsAverage = awayTargetShotsAverage
//         };
//     }
//
//     public List<MatchData> CalculateMatchAveragesDataBy(IEnumerable<Match> historicalData, DateTime upcomingMatchDate)
//     {
//         throw new NotImplementedException();
//     }
// }
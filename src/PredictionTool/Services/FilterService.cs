using Microsoft.Win32.SafeHandles;
using PredictionTool.Extensions;
using PredictionTool.Interfaces;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class FilterService: IFilterService
{
    private const string LessThanTwoGoals = "LessThanTwoGoals";
    private const string TwoToThree = "TwoToThree";
    private const string BothTeamScore = "BothTeamScore";
    private const string MoreThanTwoGoals = "MoreThanTwoGoals";
    private const string Score = "Score";
    private const string ZeroGoal = "ZeroGoal";
    
    public FilterService() { }
    
    public (string Key, double Probability) FilterGames(
        QualifiedGames qualifiedGames, List<GameProbability> gameProbabilities, List<Game> historicalGames)
    {
        var games = historicalGames
            .GetCurrentSeasonGamesBy(2022, 2023, qualifiedGames.League);

        foreach (var probability in gameProbabilities)
        {
            var zeroZeroGames = ZeroZeroGames(games, qualifiedGames.Home, qualifiedGames.Away);
            var oneSideGames = OneSideGames(games, qualifiedGames.Home, qualifiedGames.Away);
            var homeScoredAllGames = TeamScoredInLastSixGames(games, qualifiedGames.Home);
            var awayScoredAllGames = TeamScoredInLastSixGames(games, qualifiedGames.Away);

            if (zeroZeroGames)
            {
                // oneSideGames && LessThanThreeGoalsProbability(probability);
            }

            var overTwoGoals = OverTwoGoals(games, qualifiedGames.Home, qualifiedGames.Away);
            var homeMarkovChain = probability.HomeMarkovChainScoreProbability;
            var awayMarkovChain = probability.AwayMarkovChainScoreProbability;

            if (oneSideGames && !overTwoGoals)
            {
                if (probability is { Key: LessThanTwoGoals, PoissonProbability: > 0.60 })
                    return (LessThanTwoGoals, probability.PoissonProbability);
            }

            if (overTwoGoals)
            {
                if (probability is { Key: MoreThanTwoGoals, PoissonProbability: > 0.60 } ||
                    homeMarkovChain > 0.75 || awayMarkovChain > 0.75)
                    return (MoreThanTwoGoals, probability.PoissonProbability);
            }
        }
        
        return ("", 0);
    }

    private static bool ZeroZeroGames(List<Game> games, string home, string away)
    {
        var homeGames = games.GetLastSixGamesBy(home)
            .Count(i => i is { FullTimeHomeScore: 0, FullTimeAwayScore: 0 });
        
        var awayGames = games.GetLastSixGamesBy(away)
            .Count(i => i is { FullTimeHomeScore: 0, FullTimeAwayScore: 0 });

        return homeGames > 2 || awayGames > 2;
    }
    
    private static bool OneGoalFilter(List<Game> games, string home, string away)
    {
        var homeGames = games.GetLastSixGamesBy(home)
            .Count(i => i is { FullTimeHomeScore: 0, FullTimeAwayScore: 0 });
        
        var awayGames = games.GetLastSixGamesBy(away)
            .Count(i => i is { FullTimeHomeScore: 0, FullTimeAwayScore: 0 });

        return homeGames > 2 || awayGames > 2;
    }
    
    private static bool TeamScoredInLastSixGames(List<Game> games, string team)
    {
        var teamGames = games.GetLastSixGamesBy(team)
            .Count(i => i.Home == team && i.FullTimeHomeScore > 0 && i.Away == team && i.FullTimeAwayScore > 0);
        
        return teamGames == 6;
    }
    
    private static bool OneSideGames(List<Game> games, string home, string away)
    {
        var homeGames = games.GetLastSixGamesBy(home)
            .Count(i => i.FullTimeHomeScore + i.FullTimeAwayScore <= 1 &&
                             i is { FullTimeHomeScore: > 0, FullTimeAwayScore: 0 } or
                                 { FullTimeAwayScore: > 0, FullTimeHomeScore: 0 });

        var awayGames = games.GetLastSixGamesBy(away)
            .Count(i => i.FullTimeHomeScore + i.FullTimeAwayScore <= 1 &&
                        i is { FullTimeHomeScore: > 0, FullTimeAwayScore: 0 } or
                            { FullTimeAwayScore: > 0, FullTimeHomeScore: 0 });

        // if both team has at least three one side games than is true
        return homeGames >= 3 || awayGames >= 3;
    }
    
    private static bool OverTwoGoals(List<Game> games, string home, string away)
    {
        var homeGames = games.GetLastSixGamesBy(home);
        var homeOverGames = homeGames.Count(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2);
        var homeLastThreeOver = homeGames.Take(3).All(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2);
        
        var awayGames = games.GetLastSixGamesBy(away);
        var awayOverGames = awayGames.Count(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2);
        var awayLastThreeOver = awayGames.Take(3).All(i => i.FullTimeHomeScore + i.FullTimeAwayScore > 2);
        
        return (!homeLastThreeOver || !awayLastThreeOver) && homeOverGames >= 3 && awayOverGames >= 3;
    }
}
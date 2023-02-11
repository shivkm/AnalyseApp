using System.Globalization;
using AnalyseApp.Commons.Constants;
using AnalyseApp.Extensions;
using AnalyseApp.Models;
using CsvHelper;
using CsvHelper.Configuration;
using MathNet.Numerics.Distributions;

namespace AnalyseApp.Services;

public class AnalyseService
{
    private const string FileDir = "C:\\shivm\\AnalyseApp\\data";
    private List<HistoricalGame> _historicalGames = new();
    private List<HistoricalGame> _upComingGames = new();

    // Rajev
    //private const double LastSixGamesWeight = 0.40;
    //private const double HistoricalGamesWeight = 0.20;
    //private const double HeadToHeadGamesWeight = 0.25;
    //private const double PoisonProbabilityWeight = 0.15;
    // WK/SM
    private const double LastSixGamesWeight = 0.30;
    private const double HistoricalGamesWeight = 0.10;
    private const double HeadToHeadGamesWeight = 0.30;
    private const double PoisonProbabilityWeight = 0.30;
    // Shivm
    //private const double LastSixGamesWeight = 0.30;
    //private const double HistoricalGamesWeight = 0.10;
    //private const double HeadToHeadGamesWeight = 0.30;
    //private const double PoisonProbabilityWeight = 0.30;
    private List<GameProbability> Probabilities = new();

    
    internal AnalyseService ReadHistoricalGames()
    {
        var files = Directory.GetFiles($"{FileDir}\\raw_csv");

        foreach (var file in files)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, config);

            var currentFileGames = csv.GetRecords<HistoricalGame>();
            _historicalGames.AddRange(currentFileGames);
        }

        _historicalGames = _historicalGames
            .OrderByDescending(i => DateTime.Parse(i.Date)).ToList();
        return this;
    }

    internal AnalyseService CreateMlFile(string league)
    {
        var records = _historicalGames
            .Where(i => i.Div == league)
            .Select(s => new MatchData
            {
                HomeTeam = s.HomeTeam,
                AwayTeam = s.AwayTeam,
                HomeTeamGoals = Convert.ToSingle(s.FTHG),
                AwayTeamGoals = Convert.ToSingle(s.FTAG),
                HomeTeamHalfTimeGoals = Convert.ToSingle(s.HTHG),
                AwayTeamHalfTimeGoals = Convert.ToSingle(s.HTAG),
                AwayTeamShots = Convert.ToSingle(s.AS),
                HomeTeamShots = Convert.ToSingle(s.HS)
            })
            .ToList();
        using (var writer = new StreamWriter($"{FileDir}\\ml\\{league}.csv"))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteHeader<MatchData>();
            csv.NextRecord();
            foreach (var record in records)
            {
                csv.WriteRecord(record);
                csv.NextRecord();
            }
        }
        return this;
    }

    internal AnalyseService ReadUpcomingGames()
    {
        var files = Directory.GetFiles($"{FileDir}\\upcoming_matches");
        foreach (var file in files)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, config);

            var currentFileGames = csv.GetRecords<HistoricalGame>();

            _upComingGames.AddRange(currentFileGames);
        }

        _upComingGames = _upComingGames.OrderByDescending(i => i.Date).ToList();

        return this;
    }

    internal void AnalyseGames()
    {
        var poissonService = new PoissonService(_historicalGames);

        foreach (var comingGame in _upComingGames)
        {
            var gameProbability = new GameProbability
            {
                Title = $"{comingGame.HomeTeam}:{comingGame.AwayTeam}",
                Date = DateTime.Parse(comingGame.Date).Date,
                Time = DateTime.Parse(comingGame.Date),
                League = comingGame.Div
            };
            if (DerbyTeams.PopularTeams().Contains(gameProbability.Title))
                continue;

            var probability = poissonService
                .Execute(comingGame.HomeTeam, comingGame.AwayTeam, comingGame.Div)
                .OrderByDescending(i => i.Probability)
                .Take(1)
                .ToList();
            
            // Last eight games average
            var homeTeamCurrentData = _historicalGames.GetCurrentSeasonGamesBy(comingGame.HomeTeam);
            var awayTeamCurrentData = _historicalGames.GetCurrentSeasonGamesBy(comingGame.AwayTeam);

            if (probability.Any(i => i.Probability == 0.00))
                return;
            
            if (comingGame.HomeTeam == "Toulouse" || comingGame.HomeTeam == "Empoli")
            {
                var games = _historicalGames
                    .GetGameDataBy(2022, 2023);
        
                var pastMatches = _historicalGames
                    .Where(i => i.HomeTeam == comingGame.HomeTeam && i.AwayTeam == comingGame.AwayTeam ||
                                i.HomeTeam == comingGame.AwayTeam && i.AwayTeam == comingGame.HomeTeam)
                    .GetGameDataBy(2016, 2023);
                var service = new HeadToHeadService();
        
                service.HeadToHeadNaiveBayesBy(pastMatches, comingGame.HomeTeam, comingGame.AwayTeam);
                service.TeamAnalyse(games, comingGame.HomeTeam, comingGame.AwayTeam);
            }
            var headToHeads = _historicalGames.GetHeadToHeadGamesBy(comingGame.HomeTeam, comingGame.AwayTeam);
            
           
            // Current season and Poison zero zero games filter 
            if (IsZeroZero(homeTeamCurrentData, awayTeamCurrentData, headToHeads, probability))
            {
                Console.WriteLine($"{comingGame.HomeTeam}:{comingGame.AwayTeam} failed in 0:0 filter");
                continue;
            }
            
            // Both score games
            var bothScoreGames = BothTeamScore(
                homeTeamCurrentData,
                awayTeamCurrentData,
                headToHeads,
                probability);
            
            if (bothScoreGames.Qualified)
            {
                gameProbability.Msg = bothScoreGames.indicator;
                gameProbability.Qualified = bothScoreGames.Qualified;
                gameProbability.Probability = bothScoreGames.probability;
                gameProbability.ProbabilityKey = nameof(BothTeamScore);
            }
           
            // more than two score games
            var moreThanTwoGoals = MoreThanTwoScores(
                homeTeamCurrentData,
                awayTeamCurrentData,
                headToHeads,
                probability);
            
            if (moreThanTwoGoals.Qualified)
            {
                if (bothScoreGames.probability <= moreThanTwoGoals.probability)
                {
                    gameProbability.Msg = moreThanTwoGoals.Indicator;
                    gameProbability.Qualified = moreThanTwoGoals.Qualified;
                    gameProbability.Probability = moreThanTwoGoals.probability;
                    gameProbability.ProbabilityKey = nameof(MoreThanTwoScores);
                }
            }
            
            // two to three score games
            var twoToThreeGoals = TwoToThreeGoals(
                homeTeamCurrentData,
                awayTeamCurrentData,
                headToHeads,
                probability);
            
            if (!bothScoreGames.Qualified && !moreThanTwoGoals.Qualified && twoToThreeGoals.Qualified)
            {
                if (bothScoreGames.probability <= twoToThreeGoals.probability ||
                    moreThanTwoGoals.probability <= twoToThreeGoals.probability)
                {
                    gameProbability.Msg = twoToThreeGoals.Indicator;
                    gameProbability.Qualified = twoToThreeGoals.Qualified;
                    gameProbability.Probability = twoToThreeGoals.probability;
                    gameProbability.ProbabilityKey = nameof(TwoToThreeGoals);
                }
            }
           
            if (gameProbability is { Qualified: true })
                Probabilities.Add(gameProbability);
        }

        FilterBestGames();
    }

    private static (bool Qualified,double probability, string indicator) BothTeamScore(
        TeamData homeTeamCurrentData, 
        TeamData awayTeamCurrentData,
       // TeamData homeTeamData,
        // TeamData awayTeamData,
        HeadToHead headToHeads,
        IEnumerable<PoissonProbability> probabilities)
    {
        var oneGoalAverage = probabilities
            .FirstOrDefault(s => s.Key == "BothTeamScore")?.Probability ?? 0;

        if (headToHeads.GamesPlayed < 2 ||  oneGoalAverage < 0.60) 
            return (false, oneGoalAverage, "Unqualified");
        
        // If poison and last eight games pass then head to head can use range for qualify
        if ((oneGoalAverage > 0.65 && headToHeads.BothTeamScored > 0.60 || 
             oneGoalAverage > 0.68 && headToHeads.BothTeamScored > 0.40)
            && homeTeamCurrentData.OneGoalQualified && awayTeamCurrentData.OneGoalQualified)
            return (true, oneGoalAverage, "Qualified");

        return homeTeamCurrentData switch
        {
            { OneGoalQualified: false, ConcededQualified: true } when awayTeamCurrentData is
            {
                OneGoalQualified: true, ConcededQualified: true
            } => (true, oneGoalAverage, "Dangers Home failed"),
            { OneGoalQualified: true, ConcededQualified: true } when awayTeamCurrentData is
            {
                OneGoalQualified: false, ConcededQualified: true
            } => (true, oneGoalAverage, "Dangers Away failed"),
            _ => (false, oneGoalAverage, "Failed")
        };
    }
    
    private static (bool Qualified,double probability, string Indicator) MoreThanTwoScores(
        TeamData homeTeamCurrentData, 
        TeamData awayTeamCurrentData,
        HeadToHead headToHeads,
        IEnumerable<PoissonProbability> probabilities)
    {
        var moreThanTwoScores = probabilities
            .FirstOrDefault(s => s.Key == "MoreThanTwoGoals")?.Probability ?? 0;

        if (headToHeads.MoreThanTwoScored < 0.60 || headToHeads.GamesPlayed < 2 ||  moreThanTwoScores < 0.60) 
            return (false, moreThanTwoScores, "Failed");

        if (homeTeamCurrentData.OneGoalQualified && awayTeamCurrentData.OneGoalQualified && 
            homeTeamCurrentData.GoalAverage > 0.70 && awayTeamCurrentData.GoalAverage > 0.70)
            return (true, moreThanTwoScores, "Qualified");

        return homeTeamCurrentData switch
        {
            { OneGoalQualified: false, ConcededQualified: true } when awayTeamCurrentData is
            {
                OneGoalQualified: true, ConcededQualified: true
            } => (true, moreThanTwoScores, "Dangers Home failed"),
            { OneGoalQualified: true, ConcededQualified: true } when awayTeamCurrentData is
            {
                OneGoalQualified: false, ConcededQualified: true
            } => (true, moreThanTwoScores, "Dangers Away failed"),
            _ => (false, moreThanTwoScores, "Failed")
        };
    }
    
    private static (bool Qualified,double probability, string Indicator) TwoToThreeGoals(
        TeamData homeTeamCurrentData, 
        TeamData awayTeamCurrentData,
        HeadToHead headToHeads,
        IEnumerable<PoissonProbability> probabilities)
    {
        var twoToThreeGoals = probabilities
            .FirstOrDefault(s => s.Key == "TwoToThree")?.Probability ?? 0;

        if (headToHeads.TwoToThreeScored < 0.60 || headToHeads.GamesPlayed < 2 ||  twoToThreeGoals < 0.60) 
            return (false, twoToThreeGoals, "Failed");

        if ((homeTeamCurrentData.OneGoalQualified || awayTeamCurrentData.OneGoalQualified) && 
            homeTeamCurrentData.GoalAverage is < 0.70 and > 0.55 && awayTeamCurrentData.GoalAverage is < 0.70 and > 0.55)
            return (true, twoToThreeGoals, "Qualified");

        return (false, twoToThreeGoals, "Failed");
    }

    /// <summary>
    /// The method check the 0:0 games.
    /// The method retrieve first the zero zero probability of poison.
    /// After that it will check each team Data individually the 0:0 games average are bigger than 20% if so it will
    /// return true otherwise false
    /// </summary>
    /// <param name="homeTeamCurrentData"></param>
    /// <param name="homeTeamData"></param>
    /// <param name="awayTeamCurrentData"></param>
    /// <param name="awayTeamData"></param>
    /// <param name="headToHeads"></param>
    /// <param name="probabilities"></param>
    /// <returns></returns>
    private static bool IsZeroZero(
        TeamData homeTeamCurrentData, 
        //TeamData homeTeamData, 
        TeamData awayTeamCurrentData,
        //TeamData awayTeamData, 
        HeadToHead headToHeads,
        IEnumerable<PoissonProbability> probabilities)
    {
        var zeroZero = probabilities.FirstOrDefault(s => s.Key == "ZeroZeroGoals")?.Probability ?? 0;
        
        return homeTeamCurrentData is { LastAwayGameZeroZero: false, LastHomeGameZeroZero: false } && 
               awayTeamCurrentData is { LastAwayGameZeroZero: false, LastHomeGameZeroZero: false } &&
               zeroZero > 0.20 && headToHeads.BothTeamScored > 0.25;
    }

    private void FilterBestGames()
    {
        var orderedProbabilities = Probabilities
            .Where(i => i.ProbabilityKey != "Over15Goals")
            .OrderByDescending(ii => ii.Probability)
            .ToList();

        var firstTicket = new List<GameProbability>();
        var secondTicket = new List<GameProbability>();
        var thirdTicket = new List<GameProbability>();
        var dangersTicket = new List<GameProbability>();
        var otherGames = new List<GameProbability>();

        var superSicherTicket = Probabilities
            .Where(i => i.ProbabilityKey == "Over15Goals")
            .OrderByDescending(i => i.Probability)
            .ToList();

        var threeTicket = orderedProbabilities
            .Where(i => i.ProbabilityKey != "Over15Goals")
           // .Take(8)
            .OrderByDescending(i => i.Probability)
            .ToList();

        foreach (var game in threeTicket)
        {
            //var firstGameDate = firstTicket.Count == 0 ? game.Date : firstTicket[item-1].Date;
            //var secondTicketFirstGameDate = secondTicket.Count == 0 ? game.Date : secondTicket[item-1].Date;
            // (firstGameDate == game.Date || firstGameDate.AddDays(1) == game.Date) && 
            //(secondTicketFirstGameDate == game.Date || secondTicketFirstGameDate.AddDays(1) == game.Date) &&

            var game1 = game;
            if (
                firstTicket.Count(i => i.ProbabilityKey == game1.ProbabilityKey) < 2 &&
                firstTicket.Count(i => i.League == game.League) < 2 && firstTicket.Count < 4)
            {
                firstTicket.Add(game);
                continue;
            }

            if (
                secondTicket.Count(i => i.ProbabilityKey == game.ProbabilityKey) < 3 &&
                secondTicket.Count(i => i.League == game.League) < 3 && secondTicket.Count < 4 &&
                !firstTicket.Exists(i => i.Title == game.Title))
            {
                secondTicket.Add(game);
            }
        }

        foreach (var game in orderedProbabilities)
        {
            if (thirdTicket.Count(i => i.ProbabilityKey == game.ProbabilityKey) < 3 &&
                thirdTicket.Count(i => i.League == game.League) < 2 && thirdTicket.Count < 4 &&
                !firstTicket.Exists(i => i.Title == game.Title) &&
                !secondTicket.Exists(i => i.Title == game.Title))
            {
                thirdTicket.Add(game);
                continue;
            }
            
            if (dangersTicket.Count(i => i.ProbabilityKey == game.ProbabilityKey) < 2 &&
                dangersTicket.Count(i => i.League == game.League) < 2 && dangersTicket.Count < 5 &&
                firstTicket.All(i => i.Title != game.Title) &&
                secondTicket.All(i => i.Title != game.Title) &&
                thirdTicket.All(i => i.Title != game.Title))
                dangersTicket.Add(game);


            if (firstTicket.All(i => i.Title != game.Title) &&
                secondTicket.All(i => i.Title != game.Title) &&
                thirdTicket.All(i => i.Title != game.Title) &&
                dangersTicket.All(i => i.Title != game.Title))
                otherGames.Add(game);
        }

        LogTicket(firstTicket);
        LogTicket(secondTicket, 1);
        LogTicket(thirdTicket, 2);
        LogTicket(dangersTicket, 3);
        LogTicket(otherGames, 4);
    }

    private void LogTicket(List<GameProbability> topMatches, int ticketNr = 0)
    {
        var name = GetTicketName(ticketNr);
        Console.WriteLine($"###### Generated {name} Ticket #######");

        topMatches.ForEach(item =>
        {
            var isDerby = IsDerbyMatch(item.Title!);
            var message = "";
            if (isDerby)
            {
                message = "DERBY MATCH!! ";
            }

            if (item.PossibleMoreThanTwoGoals)
                message += "DANGERS!! ";

            message += $"{item.Date:d}: {item.Msg} {item.Title} {item.ProbabilityKey} = {Math.Round(item.Probability, 2)}%";

            Console.WriteLine(message);
        });
        Console.WriteLine("###############################\n\n");
    }

    private static string GetTicketName(int ticketNr)
    {
        return ticketNr switch
        {
            0 => "Super",
            1 => "Best",
            2 => "Possible",
            3 => "Dangers",
            _ => "Anything is possible"
        };
    }

    private static bool IsDerbyMatch(string title)
    {
        return DerbyTeams.GetDerbyMatches().Contains(title);
    }


    private NextGame LastEightGames(string homeTeam, string awayTeam)
    {
        var currentMatches = _historicalGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == homeTeam ||
                                   i.HomeTeam == homeTeam || i.AwayTeam == awayTeam)
            .GetGameDataBy(2022, 2023);

        var lastTenHomeGames = currentMatches
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .Take(10)
            .ToList();

        var lastTenAwayGames = currentMatches
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .Take(10)
            .ToList();

        if (!lastTenHomeGames.Any() || !lastTenAwayGames.Any())
            return new NextGame();

        var lastFourHomeHomeMatches = lastTenHomeGames
            .Where(i => i.HomeTeam == homeTeam).Take(4).ToList();

        var lastFourHomeAwayMatches = lastTenHomeGames
            .Where(i => i.AwayTeam == homeTeam).Take(4).ToList();

        var lastFourAwayHomeMatches = lastTenAwayGames
            .Where(i => i.HomeTeam == awayTeam).Take(4).ToList();

        var lastFourAwayAwayMatches = lastTenAwayGames
            .Where(i => i.AwayTeam == awayTeam).Take(4).ToList();

        var homeAway = GetTeamDataBy(lastFourHomeAwayMatches, homeTeam);
        var home = GetTeamDataBy(lastFourHomeHomeMatches, homeTeam);

        var awayHome = GetTeamDataBy(lastFourAwayHomeMatches, awayTeam);
        var away = GetTeamDataBy(lastFourAwayAwayMatches, awayTeam);

        var homeScored = GetGoalSumBy(lastTenHomeGames, homeTeam);
        var homeConceded = GetGoalSumBy(lastTenHomeGames, homeTeam, true);
        var homeAllGameCount = lastTenHomeGames.Count;

        var awayScored = GetGoalSumBy(lastTenAwayGames, awayTeam);
        var awayConceded = GetGoalSumBy(lastTenAwayGames, awayTeam, true);
        var awayAllGameCount = lastTenAwayGames.Count;

        var result = new NextGame
        {
            Home = home with
            {
                // Last ten games win accuracy
                LastTenGamesWinAccuracy = lastTenHomeGames.GetWinGamesCountBy(homeTeam).Divide(lastTenHomeGames.Count),

                // Last ten games over accuracy
                LastTenGamesOverTwoGoalsAccuracy = lastTenHomeGames
                    .GetMoreThanTwoGoalScoredGamesCount().Divide(lastTenHomeGames.Count),

                AllowGoals = homeConceded.Divide(homeAllGameCount),
                ScoredGoal = homeScored.Divide(homeAllGameCount),

                // Last ten games draw accuracy
                LastTenGamesDrawAccuracy = lastTenHomeGames
                    .Count(i => i.FTR == "D").Divide(lastTenHomeGames.Count),

                // Last five games in row over
                LastFiveGamesOver = lastTenHomeGames.Take(5).All(i => i.FTAG + i.FTHG > 2),
                LastTwoGamesWithZeroGoal = lastTenHomeGames.Take(1).Any(i => i.FTAG + i.FTHG <= 1),
                // Last five games in row both score
                LastSixGamesBothScored = lastTenHomeGames.Take(6).All(i => i is { FTAG: > 0, FTHG: > 0 }),
                LastFiveGamesLess = lastTenHomeGames.Take(5).All(i => i.FTHG + i.FTAG < 3),
                LastTwoGamesLessThanTwoGoals = lastTenHomeGames.Take(2).Any(i =>
                    (i.FTAG is 1 or 2 && i.FTHG == 0) || (i.FTHG is 1 or 2 && i.FTAG == 0)),
                BothScoreGames = home.BothScoreGames * 0.5 + homeAway.BothScoreGames * 0.5,
                MoreThanTwoGoalsGames = home.MoreThanTwoGoalsGames * 0.5 + homeAway.MoreThanTwoGoalsGames * 0.5,
                HalftimeGoalGames = home.HalftimeGoalGames * 0.5 + homeAway.HalftimeGoalGames * 0.5,
                TwoToThreeGoalGames = home.TwoToThreeGoalGames * 0.5 + homeAway.TwoToThreeGoalGames * 0.5,
                LessThanThreeGoalsAccuracy = home.LessThanThreeGoalsAccuracy * 0.5 + homeAway.LessThanThreeGoalsAccuracy * 0.5
            },
            Away = away with
            {
                // Last ten games win accuracy
                LastTenGamesWinAccuracy = lastTenAwayGames.GetWinGamesCountBy(awayTeam).Divide(lastTenAwayGames.Count),

                // Last ten games over accuracy
                LastTenGamesOverTwoGoalsAccuracy = lastTenAwayGames
                .GetMoreThanTwoGoalScoredGamesCount().Divide(lastTenAwayGames.Count),

                AllowGoals = awayConceded.Divide(awayAllGameCount),
                ScoredGoal = awayScored.Divide(awayAllGameCount),

                // Last ten games draw accuracy
                LastTenGamesDrawAccuracy = lastTenAwayGames
                .Count(i => i.FTR == "D").Divide(lastTenAwayGames.Count),

                // Last five games in row over
                LastFiveGamesOver = lastTenAwayGames.Take(5).All(i => i.FTAG + i.FTHG > 2),

                // Last five games in row both score
                LastSixGamesBothScored = lastTenAwayGames.Take(6).All(i => i is { FTAG: > 0, FTHG: > 0 }),
                LastFiveGamesLess = lastTenAwayGames.Take(5).All(i => i.FTHG + i.FTAG < 3),
                LastTwoGamesWithZeroGoal = lastFourAwayHomeMatches.Take(3).Any(i => i.FTHG < 1) &&
                                                    lastFourAwayAwayMatches.Take(3).Any(i => i.FTAG < 1),
                LastTwoGamesLessThanTwoGoals = lastTenAwayGames.Take(6).Any(i =>
                    (i.FTAG is 1 or 2 && i.FTHG == 0) || (i.FTHG is 1 or 2 && i.FTAG == 0)),
                BothScoreGames = away.BothScoreGames * 0.5 + awayHome.BothScoreGames * 0.5,
                MoreThanTwoGoalsGames = away.MoreThanTwoGoalsGames * 0.5 + awayHome.MoreThanTwoGoalsGames * 0.5,
                HalftimeGoalGames = away.HalftimeGoalGames * 0.5 + awayHome.HalftimeGoalGames * 0.5,
                TwoToThreeGoalGames = away.TwoToThreeGoalGames * 0.5 + awayHome.TwoToThreeGoalGames * 0.5,
                LessThanThreeGoalsAccuracy = away.LessThanThreeGoalsAccuracy * 0.5 + awayHome.LessThanThreeGoalsAccuracy * 0.5
            }
        };

        return result;

    }

    private NextGame LastSixSeason(string homeTeam, string awayTeam)
    {
        var homeGames = _historicalGames
            .Where(i => i.HomeTeam == homeTeam || i.AwayTeam == homeTeam)
            .ToList();

        var awayGames = _historicalGames
            .Where(i => i.HomeTeam == awayTeam || i.AwayTeam == awayTeam)
            .ToList();

        var headToHeadGames = _historicalGames
            .Where(i => i.HomeTeam == homeTeam && i.AwayTeam == awayTeam ||
                                i.HomeTeam == awayTeam && i.AwayTeam == homeTeam)
            .ToList();


        var homeScored = GetGoalSumBy(homeGames, homeTeam);
        var homeConceded = GetGoalSumBy(homeGames, homeTeam, true);
        var homeAllGameCount = homeGames.Count;

        var awayScored = GetGoalSumBy(awayGames, awayTeam);
        var awayConceded = GetGoalSumBy(awayGames, awayTeam, true);
        var awayAllGameCount = awayGames.Count;

        var homeTeamData = GetTeamDataBy(homeGames, homeTeam);
        var awayTeamData = GetTeamDataBy(awayGames, awayTeam);
        var headToHead = GetHeadToHeadDataBy(headToHeadGames);

        var result = new NextGame
        {
            Home = homeTeamData with
            {
                ScoredGoal = homeScored.Divide(homeAllGameCount),
                AllowGoals = homeConceded.Divide(homeAllGameCount)
            },
            Away = awayTeamData with
            {
                ScoredGoal = awayScored.Divide(awayAllGameCount),
                AllowGoals = awayConceded.Divide(awayAllGameCount)
            },
            HeadToHead = headToHead

        };

        return result;
    }

    private static Team GetTeamDataBy(IList<HistoricalGame> games, string team)
    {
        var gamesPlayed = games.Count;
        var teamData = new Team
        {
            GamesPlayed = games.Count,
            HalftimeGoalsScored = games.GetHalftimeGoalScoredSumBy(team).Divide(gamesPlayed),
            HalftimeGoalsConceded = games.GetHalftimeGoalConcededSumBy(team).Divide(gamesPlayed),
            // -------------------------------------------------------------------
            WinAccuracy = games.GetWinGamesCountBy(team).Divide(gamesPlayed),
            LossAccuracy = games.GetLossGamesCountBy(team).Divide(gamesPlayed),
            DrawAccuracy = games.Count(i => i.FTR == "D").Divide(gamesPlayed),
            NoGoalGames = games.GetNoGoalGameCount().Divide(gamesPlayed),
            OneSideGoalGames = games.GetOneSideGoalGamesCount().Divide(gamesPlayed),
            HalftimeGoalGames = games.GetHalftimeGoalScoredGamesCount().Divide(gamesPlayed),
            BothScoreGames = games.GetBothScoredGamesCount().Divide(gamesPlayed),
            MoreThanTwoGoalsGames = games.GetMoreThanTwoGoalScoredGamesCount().Divide(gamesPlayed),
            TwoToThreeGoalGames = games.GetTwoToThreeGoalScoredGamesCount().Divide(gamesPlayed),
            LessThanThreeGoalsAccuracy = games.Count(i => i.FTAG + i.FTHG < 3).Divide(gamesPlayed)
        };

        return teamData;
    }

    private static HeadToHead GetHeadToHeadDataBy(ICollection<HistoricalGame> games)
    {
        var gameCount = games.Count;

        var teamData = new HeadToHead
        {
            GamesPlayed = gameCount,
            HomeWin = games.Count(i => i.FTR == "H").Divide(gameCount),
            AwayWin = games.Count(i => i.FTR == "A").Divide(gameCount),
            Draw = games.Count(i => i.FTR == "D").Divide(gameCount),
            NoScored = games.GetNoGoalGameCount().Divide(gameCount),
            BothTeamScored = games.GetBothScoredGamesCount().Divide(gameCount),
            MoreThanTwoScored = games.GetMoreThanTwoGoalScoredGamesCount().Divide(gameCount),
            TwoToThreeScored = games.GetTwoToThreeGoalScoredGamesCount().Divide(gameCount),
            HalfTimeScored = games.Count(i => i.HTHG > 0 || i.HTAG > 0).Divide(gameCount),
            HomeSideScored = games.Count(i => i is { FTHG: > 0, FTAG: 0 }).Divide(gameCount),
            AwaySideScored = games.Count(i => i is { FTAG: > 0, FTHG: 0 }).Divide(gameCount),
            LessThanThreeGoal = games.Count(i => i.FTAG + i.FTHG < 3).Divide(gameCount)
        };

        return teamData;
    }

    private static int GetGoalSumBy(IReadOnlyCollection<HistoricalGame> games, string team, bool conceded = false) =>
        games.Where(i => i.HomeTeam == team).Sum(i => conceded ? i.FTAG : i.FTHG) +
        games.Where(i => i.AwayTeam == team).Sum(i => conceded ? i.FTHG : i.FTAG) ?? 0;
    private static void NoGoalAverage(double probability, NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        var homeAverage = lastSixGames.Home.NoGoalGames * LastSixGamesWeight +
                              allGames.Home.NoGoalGames * HistoricalGamesWeight +
                              allGames.HeadToHead.NoScored * HeadToHeadGamesWeight +
                              probability * PoisonProbabilityWeight;

        var awayAverage = lastSixGames.Away.NoGoalGames * LastSixGamesWeight +
                              allGames.Away.NoGoalGames * HistoricalGamesWeight +
                              allGames.HeadToHead.NoScored * HeadToHeadGamesWeight +
                              probability * PoisonProbabilityWeight;

        gameProbability.NoGoalAverage = homeAverage * 0.50 + awayAverage * 0.50;

        var limit = GetPassingLimitBy(gameProbability.League);
        if (gameProbability.NoGoalAverage > limit)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = gameProbability.NoGoalAverage;
            gameProbability.ProbabilityKey = nameof(NoGoalAverage);
        }
    }


    private static void OneSideGoalsGames(double probability, NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        var homeAverage = lastSixGames.Home.OneSideGoalGames * LastSixGamesWeight +
                          allGames.Home.OneSideGoalGames * HistoricalGamesWeight +
                          allGames.HeadToHead.HomeSideScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;

        var awayAverage = lastSixGames.Away.OneSideGoalGames * LastSixGamesWeight +
                          allGames.Away.OneSideGoalGames * HistoricalGamesWeight +
                          allGames.HeadToHead.AwaySideScored * HeadToHeadGamesWeight +
                          probability * PoisonProbabilityWeight;

        var limit = GetPassingLimitBy(gameProbability.League);
        var finalAverage = homeAverage * 0.50 + awayAverage * 0.50;
        // Home and Away both are able score two to three goals and the previous probability is not bigger than current
        // than this would be qualified.
        if (homeAverage > limit && awayAverage > limit && gameProbability.Probability < finalAverage)
        {
            gameProbability.Qualified = true;
            gameProbability.Probability = finalAverage;
            gameProbability.ProbabilityKey = nameof(OneSideGoalsGames);
        }
    }

    private static double GetPassingLimitBy(string league)
    {
        switch (league)
        {
            // Championship and Serie A
            case "E1":
            case "I1":
                return 0.58;
            // Premier und Bundesliga
            case "E0":
            case "D1":
                return 0.61;
            case "F1":
                return 0.64;
            default:
                return 0.68;
        }
    }
    private static (double, double, double) GetMonteCarloWinProbability(double probWin, double probDraw)
    {
        const int numSimulations = 1000;
        var winCount = 0;
        var drawCount = 0;
        var lossCount = 0;
        var random = new Random();

        for (var i = 0; i < numSimulations; i++)
        {
            var outcome = random.NextGaussian(probWin, 0.5);
            if (outcome <= probWin)
            {
                winCount++;
            }
            else if (outcome <= probWin + probDraw)
            {
                drawCount++;
            }
            else
            {
                lossCount++;
            }
        }

        var winProb = (double)winCount / numSimulations;
        var drawProb = (double)drawCount / numSimulations;
        var lossProb = (double)lossCount / numSimulations;

        return (winProb, drawProb, lossProb);
    }

    // Has individual weighting because the probability implemented yet.
    private static void HalfTimeScoredGames(NextGame lastSixGames, NextGame allGames, GameProbability gameProbability)
    {
        var homeAverage = lastSixGames.Home.HalftimeGoalGames * (LastSixGamesWeight + 0.15) +
                          allGames.Home.HalftimeGoalGames * (HistoricalGamesWeight + 0.10) +
                          allGames.HeadToHead.HalfTimeScored * HeadToHeadGamesWeight;

        var awayAverage = lastSixGames.Away.HalftimeGoalGames * (LastSixGamesWeight + 0.15) +
                          allGames.Away.HalftimeGoalGames * (HistoricalGamesWeight + 0.10) +
                          allGames.HeadToHead.HalfTimeScored * HeadToHeadGamesWeight;

        var halftimeGoal = homeAverage * 0.50 + awayAverage * 0.50;
        var limit = GetPassingLimitBy(gameProbability.League);
        // Home and Away both are able at least 68% to score a goal in halftime and the previous probability is not bigger than 68%
        // than this would be qualified.
        if (halftimeGoal > limit)
        {
            gameProbability.HalftimeGoalAverage = halftimeGoal;
        }
    }


    private static void TicketGenerated(List<Dictionary<string, double>> games)
    {
        var result = new Dictionary<string, double>();
        foreach (var game in games)
        {
            var orderedGames = game.OrderByDescending(i => i.Value).FirstOrDefault();
            result.Add(orderedGames.Key, orderedGames.Value);
        }

        var count = 0;
        foreach (var d in result.OrderByDescending(i => i.Value))
        {
            if (count == 0)
                Console.WriteLine("###### Generated Super Poison Ticket #######");

            if (count is 4 or 8 or 12)
                Console.WriteLine("###############################\n\n");

            if (count == 4)
                Console.WriteLine("######## Generated Second Schein ###########");

            if (count == 8)
                Console.WriteLine("######## Generated Third Schein ###########");

            Console.WriteLine(d.Key);

            count++;
        }

    }

    private static void GenerateTickets(Dictionary<string, double> gamesInOrder)
    {
        var count = 0;

        foreach (var game in gamesInOrder)
        {
            if (count == 0)
                Console.WriteLine("###### Generated Super Schein #######");

            if (count is 4 or 8 or 12)
                Console.WriteLine("###############################\n\n");


            if (count == 4)
                Console.WriteLine("######## Generated Second Schein ###########");


            if (count == 8)
                Console.WriteLine("######## Generated Third Schein ###########");



            count++;
        }
    }


    internal void Analyse(string homeTeam, string awayTeam, string league)
    {
        var service = new PoissonService(_historicalGames);
        var execute = service.Execute(homeTeam, awayTeam, league);

        // var cal = new CalculationService(service, _historicalGames, _upComingGames);
        //  cal.Execute(homeTeam, awayTeam, league);

        var list = PickTheBestProbability(homeTeam, awayTeam, execute);


    }

    private Dictionary<string, double> PickTheBestProbability(
        string homeTeam, string awayTeam,
        IList<PoissonProbability> analysePoisson, DateTime date = default, TimeSpan time = default)
    {
        date = date == default ? DateTime.Now.Date : date;
        time = time == default ? DateTime.Now.TimeOfDay : time;

        var oddProbabilitiesInDescOrder = analysePoisson
            .Where(ia => ia.Key is "AwayWin" or "HomeWin" or "Draw")
            .OrderByDescending(ii => ii.Probability)
            .ToList();

        var probabilitiesInDescOrder = analysePoisson
            .OrderByDescending(ii => ii.Probability)
            .Take(1)
            .ToList();

        var list = new Dictionary<string, double>();
        probabilitiesInDescOrder.ForEach(item =>
        {

            var match = $"{homeTeam}:{awayTeam}";
            if (DerbyTeams.GetDerbyMatches().Contains(match))
                match = $"DERBY MATCH!! {match}";

            switch (item.Key)
            {
                case nameof(HistoricalGame.HomeWin):
                    if (item.Probability >= 0.70)
                    {
                        var msg = $"{date:d} {time:g}: {match} Home win = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.AwayWin):
                    if (item.Probability >= 0.70)
                    {

                        var msg = $"{date:d} {time:g}: {match} Away win = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.Draw):
                    if (item.Probability >= 0.68)
                    {
                        var msg = $"{date:d} {time:g}: {match} Draw =  {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.BothTeamScore):
                    if (item.Probability > 0.64)
                    {
                        var msg = $"{date:d} {time:g}: {match} Both score = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.MoreThanTwoGoals):
                    if (item.Probability > 0.60)
                    {
                        var msg = $"{date:d} {time:g}: {match} More Than two goals = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.TwoToThree):
                    if (item.Probability > 0.60)
                    {
                        var msg = $"{date:d} {time:g}: {match} Two to three goals = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.LessThanTwoGoals):
                    if (item.Probability > 0.70)
                    {
                        var msg = $"{date:d} {time:g}: {match} less than three goals = {Math.Round(item.Probability, 2)}%";
                        list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
            }
        });

        oddProbabilitiesInDescOrder.ForEach(item =>
        {
            var match = $"{homeTeam}:{awayTeam}";
            if (DerbyTeams.GetDerbyMatches().Contains(match))
                match = $"DERBY MATCH!! {match}";

            switch (item.Key)
            {
                case nameof(HistoricalGame.HomeWin):
                    if (item.Probability > 0.70)
                    {
                        var msg = $"{date:d} {time:g}: {match} Home win = {Math.Round(item.Probability, 2)}%";
                        //list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.AwayWin):
                    if (item.Probability >= 0.70)
                    {

                        var msg = $"{date:d} {time:g}: {match} Away win = {Math.Round(item.Probability, 2)}%";
                        //list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
                case nameof(HistoricalGame.Draw):
                    if (item.Probability >= 0.68)
                    {
                        var msg = $"{date:d} {time:g}: {match} Draw =  {Math.Round(item.Probability, 2)}%";
                        // list.Add(msg, item.Probability);
                        Console.WriteLine(msg);
                    }
                    break;
            }

        });

        return list;
    }
}
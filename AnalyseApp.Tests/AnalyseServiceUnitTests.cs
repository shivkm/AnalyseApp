using System.ComponentModel;
using Accord;
using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Options;
using AnalyseApp.Services;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace AnalyseApp.Tests;

public class AnalyseServiceUnitTests
{
    private readonly IMatchPredictor _matchPredictor;
    private readonly ITestOutputHelper _testOutputHelper;
    
    private const string overTwoGoals = "Over Tow Goals";
    private const string underThreeGoals = "Under Three Goals";
    private const string bothTeamScore = "Both Team Score Goals";
    private const string twoToThreeGoals = "Two to three Goals";
    private const string HomeWin = "Home will win";
    private const string AwayWin = "Away will win";
    private const string BothTeamScore = "Both Team Score Goals";

    public AnalyseServiceUnitTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions 
        {
            RawCsvDir = "C:\\shivm\\AnalyseApp\\data\\raw_csv",
            AnalyseResult = "C:\\shivm\\AnalyseApp\\data\\analysed_result"
        };

        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        var fileProcessor = new FileProcessor(optionsWrapper);
        
        _matchPredictor = new MatchPredictor(fileProcessor, new PoissonService(), new DataService(fileProcessor));
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void Premier_League_Game_State_Preparation()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCoung = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = "Burnley", AwayTeam = "Man City", Date = "11/08/2023", FTHG = 0, FTAG = 3 },
            new() { HomeTeam = "Almeria", AwayTeam = "Vallecano", Date = "11/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Sevilla", AwayTeam = "Valencia", Date = "11/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Amorebieta", AwayTeam = "Levante", Date = "11/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Valladolid", AwayTeam = "Sporting", Date = "11/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Nice", AwayTeam = "Lille", Date = "11/08/2023", FTHG = 1, FTAG = 1 },
            
            new() { HomeTeam = "Arsenal", AwayTeam = PremierLeague.NottmForest.GetDescription(), Date = "12/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Bournemouth", AwayTeam = "West Ham", Date = "12/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Brighton", AwayTeam = "Luton", Date = "12/08/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = "Everton", AwayTeam = "Fulham", Date = "12/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Sheffield United", AwayTeam = "Crystal Palace", Date = "12/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Newcastle", AwayTeam = "Aston Villa", Date = "12/08/2023", FTHG = 5, FTAG = 1 },
            
            new() { HomeTeam = "Coventry", AwayTeam = "Middlesbrough", Date = "12/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Birmingham", AwayTeam = "Leeds", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Cardiff", AwayTeam = "QPR", Date = "12/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Ath Bilbao", AwayTeam = "Real Madrid", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Sociedad", AwayTeam = "Girona", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Las Palmas", AwayTeam = "Mallorca", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Marseille", AwayTeam = "Reims", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Huddersfield", AwayTeam = "Leicester", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Hull", AwayTeam = "Scheffield Wed", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Ipswich", AwayTeam = "Stoke", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Millwall", AwayTeam = "Bristol City", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Preston", AwayTeam = "Sunderland", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Rotherham", AwayTeam = "Blackburn", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Southampton", AwayTeam = "Norwich", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Watford", AwayTeam = "Plymouth", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "West Brom", AwayTeam = "Swansea", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Santander", AwayTeam = "Eiber", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Zaragoza", AwayTeam = "Villarreal B", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Eiche", AwayTeam = "Ferrol", Date = "12/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Schalke 04", AwayTeam = "Holstein Kiel", Date = "25/08/2023", FTHG = 0, FTAG = 4 },
            new() { HomeTeam = "Sampdoria", AwayTeam = "Pisa", Date = "25/08/2023", FTHG = 3, FTAG = 2 },
            new() { HomeTeam = "Celta", AwayTeam = "Real Madrid", Date = "25/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = PremierLeague.NottmForest.GetDescription(), AwayTeam = "Sheffield United", Date = "18/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Leeds", AwayTeam = "West Brom", Date = "18/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Mallorca", AwayTeam = "Villarreal", Date = "18/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Valencia", AwayTeam = "Las Palmas", Date = "18/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Andorra", AwayTeam = "Cartagena", Date = "18/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = "Zaragoza", AwayTeam = "Valladolid", Date = "18/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Metz", AwayTeam = "Marseille", Date = "18/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Bari", AwayTeam = "Palermo", Date = "18/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "RB Leipzig", AwayTeam = "Stuttgart", Date = "24/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Fulham", AwayTeam = "Brentford", Date = "18/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Bari", AwayTeam = "Palermo", Date = "18/08/2023", FTHG = 0, FTAG = 0 },
        };

       
        // ACTUAL ASSERT
        foreach (var game in upcomingMatches)
        {
            totalCount++;
            var actual = _matchPredictor.Execute(
                game.HomeTeam, 
                game.AwayTeam,
                game.Date
            );

            var isCorrect = CorrectCount(actual, game);
            if (isCorrect)
            {
                correctCount++;
            }
            else
            {
                wrongCoung++;
            }

            _testOutputHelper.WriteLine($"{game.Date} - {game.HomeTeam}:{game.AwayTeam}{actual.Msg}");
        }

        var correctQuote = correctCount / totalCount * 100;
        _testOutputHelper.WriteLine($"Count: {totalCount} und die Richtigkeit Quote ist : {correctQuote}");
    }

    private static bool CorrectCount(Prediction actual, Matches game)
    {
        if (actual.Msg == bothTeamScore && game.FTHG is > 0 and > 0)
        {
            return true;
        }

        if (actual.Msg == overTwoGoals && game.FTHG + game.FTAG > 2)
        {
            return true;
        }

        if (actual.Msg == underThreeGoals && game.FTHG + game.FTAG < 3)
        {
            return true;
        }

        if (actual.Msg == twoToThreeGoals && game.FTHG + game.FTAG == 3 || game.FTHG + game.FTAG == 2)
        {
            return true;
        }

        if (actual.Msg == HomeWin && game.FTHG > game.FTAG)
        {
            return true;
        }

        if (actual.Msg == AwayWin && game.FTAG > game.FTHG)
        {
            return true;
        }
        return false;
    }

    [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void Premier_League_Game_State_Preparation2()
    {
        // ARRANGE
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = "Leverkusen", AwayTeam = "RB Leipzig", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Augsburg", AwayTeam = "M'gladbach", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Hoffenheim", AwayTeam = "Freiburg", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Stuttgart", AwayTeam = "Bochum", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Wolfsburg", AwayTeam = "Heidenheim", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Dortmund", AwayTeam = "FC Koln", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Union Berlin", AwayTeam = "Mainz", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Ein Frankfurt", AwayTeam = "Darmstadt", Date = "18/08/2023", FTHG = 1, FTAG = 5 },
            
            new() { HomeTeam = "Hansa Rostock", AwayTeam = "Hannover", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Greuther Furth", AwayTeam = "St Pauli", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Fortuna Dusseldorf", AwayTeam = "Paderborn", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Hamburg", AwayTeam = "Hertha", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Osnabruck", AwayTeam = "Nurnberg", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Holstein Kiel", AwayTeam = "Magdeburg", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Braunschweig", AwayTeam = "Schalke", Date = "20/08/2023", FTHG = 1, FTAG = 5 },

            new() { HomeTeam = "Fulham", AwayTeam = "Brentford", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Liverpool", AwayTeam = "Bournemouth", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Wolves", AwayTeam = "Brighton", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Man City", AwayTeam = "Newcastle", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Tottenham", AwayTeam = "Man United", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "West Ham", AwayTeam = "Chelsea", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Aston Villa", AwayTeam = "Everton", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Crystal Palace", AwayTeam = "Arsenal", Date = "21/08/2023", FTHG = 1, FTAG = 5 },

            new() { HomeTeam = "Plymouth", AwayTeam = "Southampton", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Blackburn", AwayTeam = "Hull", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Bristol City", AwayTeam = "Birmingham", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Leicester", AwayTeam = "Cardiff", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Middlesbrough", AwayTeam = "Huddersfield", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "QPR", AwayTeam = "Ipswich", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Sheffield Weds", AwayTeam = "Preston", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Stoke", AwayTeam = "Watford", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Sunderland", AwayTeam = "Rotherham", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Swansea", AwayTeam = "Coventry", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Norwich", AwayTeam = "Millwall", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            
            new() { HomeTeam = "Bolton", AwayTeam = "Wigan", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Barnsley", AwayTeam = "Oxford", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Blackpool", AwayTeam = "Leyton Orient", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Cambridge", AwayTeam = "Bristol Rvs", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Carlisle", AwayTeam = "Exeter", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Charlton", AwayTeam = "Port Vale", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Derby", AwayTeam = "Fleetwood Town", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Northampton", AwayTeam = "Peterboro", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Portsmouth", AwayTeam = "Cheltenham", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Shrewsbury", AwayTeam = "Lincoln", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Wycombe", AwayTeam = "Burton", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            
            new() { HomeTeam = "Sociedad", AwayTeam = "Celta", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Almeria", AwayTeam = "Real Madrid", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Osasuna", AwayTeam = "Ath Bilbao", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Girona", AwayTeam = "Getafe", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Barcelona", AwayTeam = "Cadiz", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Betis", AwayTeam = "Ath Madrid", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Granada", AwayTeam = "Vallecano", Date = "21/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Alaves", AwayTeam = "Sevilla", Date = "21/08/2023", FTHG = 1, FTAG = 5 },
            
            new() { HomeTeam = "Eibar", AwayTeam = "Elche", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Espanol", AwayTeam = "Santander", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Levante", AwayTeam = "Burgos", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Alcorcon", AwayTeam = "Leganes", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Oviedo", AwayTeam = "Ferrol", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Sp Gijon", AwayTeam = "Mirandes", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Albacete", AwayTeam = "Amorebieta", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Villarreal B", AwayTeam = "Eldense", Date = "21/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Huesca", AwayTeam = "Tenerife", Date = "21/08/2023", FTHG = 1, FTAG = 5 },
            
            new() { HomeTeam = "Cosenza", AwayTeam = "Ascoli", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Cremonese", AwayTeam = "Catanzaro", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Ternana", AwayTeam = "Sampdoria", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Sudtirol", AwayTeam = "Spezia", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Cittadella", AwayTeam = "Reggiana", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Parma", AwayTeam = "FeralpiSalo", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Venezia", AwayTeam = "Como", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            
            new() { HomeTeam = "Empoli", AwayTeam = "Verona", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Frosinone", AwayTeam = "Napoli", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Genoa", AwayTeam = "Fiorentina", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Inter", AwayTeam = "Monza", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Roma", AwayTeam = "Salernitana", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Sassuolo", AwayTeam = "Atalanta", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Lecce", AwayTeam = "Lazio", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Udinese", AwayTeam = "Juventus", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            
            new() { HomeTeam = "Lyon", AwayTeam = "Montpellier", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Toulouse", AwayTeam = "Paris SG", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Lille", AwayTeam = "Nantes", Date = "30/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Le Havre", AwayTeam = "Brest", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Lorient", AwayTeam = "Nice", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Reims", AwayTeam = "Clermont", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Monaco", AwayTeam = "Strasbourg", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Lens", AwayTeam = "Rennes", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            
            new() { HomeTeam = "Angers", AwayTeam = "Auxerre", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Amiens", AwayTeam = "Bastia", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Annecy", AwayTeam = "Dunkerque", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Concarneau", AwayTeam = "Caen", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Grenoble", AwayTeam = "Troyes", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Laval", AwayTeam = "Rodez", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Pau FC", AwayTeam = "Paris FC", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "St Etienne", AwayTeam = "Quevilly Rouen", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Valenciennes", AwayTeam = "Guingamp", Date = "19/08/2023", FTHG = 1, FTAG = 5 }
        };

        var count = 0;
        // ACTUAL ASSERT
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            if (actual.Msg is not "")
            {
                count++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam}{actual.Msg}");
            }
        }
        _testOutputHelper.WriteLine($"Count: {count}");
    }
    
    
    [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void BothTeamScoreGoal_Analysis()
    {
        // ARRANGE
        var totalCount = 0;
        var correctCount = 0;
        var wrongCount = 0;
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = "Hoffenheim", AwayTeam = "Freiburg", Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Union Berlin", AwayTeam = "Mainz", Date = "18/08/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = "Ein Frankfurt", AwayTeam = "Darmstadt", Date = "18/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Hansa Rostock", AwayTeam = "Hannover", Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Osnabruck", AwayTeam = "Nurnberg", Date = "19/08/2023", FTHG = 2, FTAG = 3 },
            new() { HomeTeam = "Holstein Kiel", AwayTeam = "Magdeburg", Date = "19/08/2023", FTHG = 2, FTAG = 4 },
            new() { HomeTeam = "Liverpool", AwayTeam = "Bournemouth", Date = "18/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = "Tottenham", AwayTeam = "Man United", Date = "18/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "West Ham", AwayTeam = "Chelsea", Date = "19/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = "Crystal Palace", AwayTeam = "Arsenal", Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Blackburn", AwayTeam = "Hull", Date = "19/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Hamburg", AwayTeam = "Hertha", Date = "19/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Fulham", AwayTeam = "Brentford", Date = "19/08/2023", FTHG = 0, FTAG = 3 },
            new() { HomeTeam = "Wolves", AwayTeam = "Brighton", Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = "Man City", AwayTeam = "Newcastle", Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Aston Villa", AwayTeam = "Everton", Date = "20/08/2023", FTHG = 4, FTAG = 0 },
            new() { HomeTeam = "Plymouth", AwayTeam = "Southampton", Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Bristol City", AwayTeam = "Birmingham", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Leicester", AwayTeam = "Cardiff", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Middlesbrough", AwayTeam = "Huddersfield", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Sheffield Weds", AwayTeam = "Preston", Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Stoke", AwayTeam = "Watford", Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Sunderland", AwayTeam = "Rotherham", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Swansea", AwayTeam = "Coventry", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Norwich", AwayTeam = "Millwall", Date = "20/08/2023", FTHG = 3, FTAG = 1 },
            
            new() { HomeTeam = "Sociedad", AwayTeam = "Celta", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Almeria", AwayTeam = "Real Madrid", Date = "19/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Osasuna", AwayTeam = "Ath Bilbao", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Girona", AwayTeam = "Getafe", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Barcelona", AwayTeam = "Cadiz", Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Betis", AwayTeam = "Ath Madrid", Date = "20/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Granada", AwayTeam = "Vallecano", Date = "21/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Alaves", AwayTeam = "Sevilla", Date = "21/08/2023", FTHG = 4, FTAG = 3 },
            new() { HomeTeam = "Eibar", AwayTeam = "Elche", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Espanol", AwayTeam = "Santander", Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Levante", AwayTeam = "Burgos", Date = "19/08/2023", FTHG = 3, FTAG = 2 },
            new() { HomeTeam = "Alcorcon", AwayTeam = "Leganes", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Oviedo", AwayTeam = "Ferrol", Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Sp Gijon", AwayTeam = "Mirandes", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Albacete", AwayTeam = "Amorebieta", Date = "20/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Villarreal B", AwayTeam = "Eldense", Date = "21/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = "Huesca", AwayTeam = "Tenerife", Date = "21/08/2023", FTHG = 0, FTAG = 2 },
            
            new() { HomeTeam = "Cosenza", AwayTeam = "Ascoli", Date = "19/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Cremonese", AwayTeam = "Catanzaro", Date = "19/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Ternana", AwayTeam = "Sampdoria", Date = "19/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Sudtirol", AwayTeam = "Spezia", Date = "20/08/2023", FTHG = 3, FTAG = 3 },
            new() { HomeTeam = "Cittadella", AwayTeam = "Reggiana", Date = "20/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Parma", AwayTeam = "FeralpiSalo", Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Venezia", AwayTeam = "Como", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Empoli", AwayTeam = "Verona", Date = "19/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Frosinone", AwayTeam = "Napoli", Date = "19/08/2023", FTHG = 1, FTAG = 3 },
            new() { HomeTeam = "Genoa", AwayTeam = "Fiorentina", Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = "Inter", AwayTeam = "Monza", Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Roma", AwayTeam = "Salernitana", Date = "20/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Sassuolo", AwayTeam = "Atalanta", Date = "20/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Lecce", AwayTeam = "Lazio", Date = "20/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Udinese", AwayTeam = "Juventus", Date = "20/08/2023", FTHG = 0, FTAG = 3 },
            
            new() { HomeTeam = "Lyon", AwayTeam = "Montpellier", Date = "19/08/2023", FTHG = 1, FTAG = 4 },
            new() { HomeTeam = "Toulouse", AwayTeam = "Paris SG", Date = "19/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Lille", AwayTeam = "Nantes", Date = "30/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Le Havre", AwayTeam = "Brest", Date = "20/08/2023", FTHG = 1, FTAG = 2 },
            new() { HomeTeam = "Lorient", AwayTeam = "Nice", Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Reims", AwayTeam = "Clermont", Date = "20/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "Monaco", AwayTeam = "Strasbourg", Date = "20/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Lens", AwayTeam = "Rennes", Date = "20/08/2023", FTHG = 1, FTAG = 1 },
            
            new() { HomeTeam = "Angers", AwayTeam = "Auxerre", Date = "19/08/2023", FTHG = 2, FTAG = 2 },
            new() { HomeTeam = "Amiens", AwayTeam = "Bastia", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Annecy", AwayTeam = "Dunkerque", Date = "19/08/2023", FTHG = 3, FTAG = 0 },
            new() { HomeTeam = "Concarneau", AwayTeam = "Caen", Date = "19/08/2023", FTHG = 0, FTAG = 2 },
            new() { HomeTeam = "Grenoble", AwayTeam = "Troyes", Date = "19/08/2023", FTHG = 0, FTAG = 0 },
            new() { HomeTeam = "Laval", AwayTeam = "Rodez", Date = "19/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Pau FC", AwayTeam = "Paris FC", Date = "19/08/2023", FTHG = 2, FTAG = 0 },
            new() { HomeTeam = "St Etienne", AwayTeam = "Quevilly Rouen", Date = "19/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Valenciennes", AwayTeam = "Guingamp", Date = "19/08/2023", FTHG = 0, FTAG = 0 }
        };

        // ACTUAL ASSERT
        foreach (var lastSixGame in upcomingMatches)
        {
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            var isCorrect = lastSixGame.FTAG + lastSixGame.FTHG > 2 && actual.Type == BetType.OverTwoGoals ||
                            lastSixGame is { FTHG: > 0, FTAG: > 0 } && actual.Type == BetType.BothTeamScoreGoals ||
                            lastSixGame.FTAG + lastSixGame.FTHG < 3 && actual.Type == BetType.UnderThreeGoals ||
                            lastSixGame.FTAG + lastSixGame.FTHG == 2 || lastSixGame.FTAG + lastSixGame.FTHG == 3 && actual.Type == BetType.TwoToThreeGoals;

            if (actual.Qualified)
            {
                if (isCorrect)
                {
                    correctCount++;
                }
                else
                {
                    wrongCount++;
                    _testOutputHelper.WriteLine($" ----  {lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} ---- ");
                }
                totalCount++;
                _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam} {actual.Msg}");
            }
        }
        _testOutputHelper.WriteLine($"Count: {totalCount}, correct count: {correctCount}, wrong count: {wrongCount}");
    }
    
    
        
    [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void Premier_League_Game_State_Preparation3()
    {
        // ARRANGE
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = "Clermont Foot", AwayTeam = "Metz", Date = "27/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Valencia", AwayTeam = "Osasuna", Date = "27/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Montpellier", AwayTeam = "Reims", Date = "24/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Strasbourg", AwayTeam = "Toulouse", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Auxerre", AwayTeam = "Grenoble", Date = "21/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Plymouth", AwayTeam = "Southampton", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Blackburn", AwayTeam = "Hull", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "QPR", AwayTeam = "Ipswich", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Norwich", AwayTeam = "Millwall", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Bari", AwayTeam = "Palermo", Date = "18/08/2023", FTHG = 0, FTAG = 0 }
        };

        var count = 0;
       
        // ACTUAL ASSERT
        foreach (var lastSixGame in upcomingMatches)
        {
            count++;
            var actual = _matchPredictor.Execute(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );
            _testOutputHelper.WriteLine($"{lastSixGame.Date} - {lastSixGame.HomeTeam}:{lastSixGame.AwayTeam}{actual.Msg}");
        }
        _testOutputHelper.WriteLine($"Count: {count}");
    }
    
    
}
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

    public AnalyseServiceUnitTests(ITestOutputHelper testOutputHelper)
    {
        var fileProcessorOptions = new FileProcessorOptions 
        {
            RawCsvDir = "C:\\shivm\\AnalyseApp\\data\\raw_csv",
            AnalyseResult = "C:\\shivm\\AnalyseApp\\data\\analysed_result"
        };

        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        var fileProcessor = new FileProcessor(optionsWrapper);
        var historicalData = fileProcessor.GetHistoricalMatchesBy();
        
        _matchPredictor = new MatchPredictor(historicalData, new PoissonService(), new DataService(historicalData));
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void Premier_League_Game_State_Preparation()
    {
        // ARRANGE
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = "Werder Bremen", AwayTeam = "Bayern Munich", Date = "18/08/2023", FTHG = 0, FTAG = 4 },
            new() { HomeTeam = "Kaiserlautern", AwayTeam = "Elversberg", Date = "18/08/2023", FTHG = 3, FTAG = 2 },
            new() { HomeTeam = "Wehen", AwayTeam = "Karlsruhe", Date = "18/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = PremierLeague.NottmForest.GetDescription(), AwayTeam = "Sheffield United", Date = "18/08/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = "Leeds", AwayTeam = "West Brom", Date = "18/08/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = "Mallorca", AwayTeam = "Villarreal", Date = "18/08/2023", FTHG = 0, FTAG = 1 },
            new() { HomeTeam = "Valencia", AwayTeam = "Las Palmas", Date = "18/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Andorra", AwayTeam = "Cartagena", Date = "18/08/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = "Zaragoza", AwayTeam = "Valladolid", Date = "18/08/2023", FTHG = 1, FTAG = 0 },
            new() { HomeTeam = "Metz", AwayTeam = "Marseille", Date = "18/08/2023", FTHG = 2, FTAG = 2 },
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
    public void Premier_League_Game_State_Preparation3()
    {
        // ARRANGE
        var upcomingMatches = new List<Matches>
        {
            new() { HomeTeam = "Greuther Furth", AwayTeam = "St Pauli", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Osnabruck", AwayTeam = "Nurnberg", Date = "20/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Man City", AwayTeam = "Newcastle", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Tottenham", AwayTeam = "Man United", Date = "19/08/2023", FTHG = 1, FTAG = 5 },
            new() { HomeTeam = "Crystal Palace", AwayTeam = "Arsenal", Date = "21/08/2023", FTHG = 1, FTAG = 5 },
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
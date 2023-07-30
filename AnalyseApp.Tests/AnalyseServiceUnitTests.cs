using System.ComponentModel;
using Accord;
using AnalyseApp.Enums;
using AnalyseApp.Interfaces;
using AnalyseApp.models;
using AnalyseApp.Options;
using AnalyseApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AnalyseApp.Tests;

public class AnalyseServiceUnitTests
{
    private readonly IAnalyseService _analyseService;
    private readonly IPredictService _predictService;

    public AnalyseServiceUnitTests()
    {
        var fileProcessorOptions = new FileProcessorOptions 
        {
            RawCsvDir = "C:\\shivm\\AnalyseApp\\data\\raw_csv",
            AnalyseResult = "C:\\shivm\\AnalyseApp\\data\\analysed_result"
        };

        // Wrap the instance in OptionsWrapper
        var optionsWrapper = new OptionsWrapper<FileProcessorOptions>(fileProcessorOptions);
        _analyseService = new AnalyseService(new FileProcessor(optionsWrapper));
        _predictService = new PredictService(new FileProcessor(optionsWrapper));
    }
    
    [Fact,
     Description("Premier league games qualified the first level analysis")]
    public void Premier_League_Game_State_Preparation()
    {
        // ARRANGE
        var brighton = PremierLeague.Brighton.GetDescription();

        var lastSixGames = new List<Matches>
        {
            new() { HomeTeam = PremierLeague.AstonVilla.GetDescription(), AwayTeam = brighton, Date = "28/05/2023", FTHG = 2, FTAG = 1 },
            new() { HomeTeam = brighton, AwayTeam = PremierLeague.ManCity.GetDescription(), Date = "24/05/2023", FTHG = 1, FTAG = 1 },
            new() { HomeTeam = brighton, AwayTeam = PremierLeague.Southampton.GetDescription(), Date = "21/05/2023", FTHG = 3, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Newcastle.GetDescription(), AwayTeam = brighton, Date = "18/05/2023", FTHG = 4, FTAG = 1 },
            new() { HomeTeam = PremierLeague.Arsenal.GetDescription(), AwayTeam = brighton, Date = "14/05/2023", FTHG = 0, FTAG = 3 },
            new() { HomeTeam = brighton, AwayTeam = PremierLeague.Everton.GetDescription(), Date = "08/05/2023", FTHG = 1, FTAG = 5 }
        };
       
        // ACTUAL ASSERT
        foreach (var lastSixGame in lastSixGames)
        {
            var actual = _predictService.OverUnderPredictionBy(
                lastSixGame.HomeTeam, 
                lastSixGame.AwayTeam,
                lastSixGame.Date
            );

            if (lastSixGame.FTAG + lastSixGame.FTHG > 2) actual.Should().Be("Over");
            if (lastSixGame.FTAG + lastSixGame.FTHG < 3) actual.Should().Be("Under");
        }
    }

    [Fact]
    public void Premier_league_Match_Expected_Over_Two_Goals()
    {
        // ARRANGE
        const double homeScoreAvg = 0.875;
        const double homeConcededAvg = 1.0;
        const double awayScoreAvg = 1.5;
        const double awayConcededAvg = 1.875;

        var sessionAnalysis = _analyseService.Test2(
            homeScoreAvg, homeConcededAvg, 
            awayScoreAvg, awayConcededAvg
        );
        
        var lastSixHomeAnalysis = _analyseService.Test2(
            1.333333333, 1.333333333, 
            0.3333333333, 1.333333333
        );
        
        var lastSixAwayAnalysis = _analyseService.Test2(
            2.333333333, 2.333333333, 
            2.333333333, 2
        );
        
        var headToHeadAnalysis = _analyseService.Test2(
            2.333333333, 1.666666667,
            1.333333333,1
        );

        var scoreProbability = (sessionAnalysis.scoreProbability + lastSixHomeAnalysis.scoreProbability +
                                lastSixAwayAnalysis.scoreProbability + headToHeadAnalysis.scoreProbability) / 4;

        
        var noScoreProbability = (sessionAnalysis.noScoreProbability + lastSixHomeAnalysis.noScoreProbability +
                                  lastSixAwayAnalysis.noScoreProbability + headToHeadAnalysis.noScoreProbability) / 4;
        
        if (scoreProbability > 0.75 && noScoreProbability < 0.25)
        {
            // sure over 2.5 goals
        }
        
        if (scoreProbability > 0.64 && noScoreProbability < 0.36)
        {
            // over 2.5 goals
        }
    }
    
    [Fact]
    public void Premier_league_Match_Expected_Over_Two_Goals2()
    {
        // ARRANGE
        var sessionAnalysis = _analyseService.Test2(
            3.142857143,3.142857143, 3.142857143,3.142857143
        );
        
        var lastSixHomeAnalysis = _analyseService.Test2(
            3.142857143,	3.142857143,	3.142857143,	3.142857143
        );
        
        var lastSixAwayAnalysis = _analyseService.Test2(
            3.142857143,	3.142857143,	3.142857143,	3.142857143
        );
        
        var headToHeadAnalysis = _analyseService.Test2(
            3.142857143,	3.142857143,	3.142857143,	3.142857143
        );

        var scoreProbability = (sessionAnalysis.scoreProbability + lastSixHomeAnalysis.scoreProbability +
                                lastSixAwayAnalysis.scoreProbability + headToHeadAnalysis.scoreProbability) / 4;

        
        var noScoreProbability = (sessionAnalysis.noScoreProbability + lastSixHomeAnalysis.noScoreProbability +
                                lastSixAwayAnalysis.noScoreProbability + headToHeadAnalysis.noScoreProbability) / 4;
        
        if (scoreProbability > 0.64 && noScoreProbability < 0.36)
        {
            // over 2.5 goals
        }
    }
    
    [Fact]
    public void Premier_league_Match_Expected_Over_Two_Goals3()
    {
        // ARRANGE
        var sessionAnalysis = _analyseService.Test2(
            1.125,	1.125,	1.875,	1.625
        );
        
        var lastSixHomeAnalysis = _analyseService.Test2(
            0	,2	,2.25	,0.5
        );
        
        var lastSixAwayAnalysis = _analyseService.Test2(
            1,	1,	2.333333333,	1.333333333
        );
        
        var headToHeadAnalysis = _analyseService.Test2(
            2	,1.5,	1.5,	0.5
        );

        var scoreProbability = (sessionAnalysis.scoreProbability + lastSixHomeAnalysis.scoreProbability +
                                lastSixAwayAnalysis.scoreProbability + headToHeadAnalysis.scoreProbability) / 4;

        
        var noScoreProbability = (sessionAnalysis.noScoreProbability + lastSixHomeAnalysis.noScoreProbability +
                                  lastSixAwayAnalysis.noScoreProbability + headToHeadAnalysis.noScoreProbability) / 4;
        
        if (scoreProbability > 0.64 && noScoreProbability < 0.36)
        {
            // over 2.5 goals
        }

        if (sessionAnalysis.scoreProbability > 0.60 && headToHeadAnalysis.scoreProbability > 0.60 &&
            lastSixHomeAnalysis.scoreProbability < 0.50 && lastSixAwayAnalysis.scoreProbability > 0.60 &&
            noScoreProbability is > 0.25 and < 0.40)
        {
            // away will score
        }
        
        
        if (sessionAnalysis.scoreProbability > 0.60 && headToHeadAnalysis.scoreProbability > 0.60 &&
            lastSixHomeAnalysis.scoreProbability > 0.60 && lastSixAwayAnalysis.scoreProbability < 0.50 &&
            noScoreProbability is > 0.25 and < 0.40)
        {
            // home will score
        }
    }
    
    [Fact]
    public void Premier_league_Match_Expected_Over_Two_Goals4()
    {
        // ARRANGE
        var sessionAnalysis = _analyseService.Test2(
            1.71,	0.571,	1.5,	1.125
        );
        
        var lastSixHomeAnalysis = _analyseService.Test2(
            2,0,1.25,1.25
        );
        
        var lastSixAwayAnalysis = _analyseService.Test2(
            1.6666666667,	1.6666666667,	1,	2.66666666667
        );
        
        var headToHeadAnalysis = _analyseService.Test2(
            3.3333333333,1,	1.33333333333,	0.6666666667
        );

        var scoreProbability = (sessionAnalysis.scoreProbability + lastSixHomeAnalysis.scoreProbability +
                                lastSixAwayAnalysis.scoreProbability + headToHeadAnalysis.scoreProbability) / 4;

        
        var noScoreProbability = (sessionAnalysis.noScoreProbability + lastSixHomeAnalysis.noScoreProbability +
                                  lastSixAwayAnalysis.noScoreProbability + headToHeadAnalysis.noScoreProbability) / 4;
        
        if (scoreProbability > 0.64 && noScoreProbability < 0.36)
        {
            // over 2.5 goals
        }

        if (sessionAnalysis.scoreProbability > 0.60 && headToHeadAnalysis.scoreProbability > 0.60 &&
            lastSixHomeAnalysis.scoreProbability < 0.50 && lastSixAwayAnalysis.scoreProbability > 0.60 &&
            noScoreProbability is > 0.25 and < 0.40)
        {
            // away will score
        }
        
        
        if (sessionAnalysis.scoreProbability > 0.60 && headToHeadAnalysis.scoreProbability > 0.60 &&
            lastSixHomeAnalysis.scoreProbability > 0.60 && lastSixAwayAnalysis.scoreProbability > 0.60 &&
            noScoreProbability is > 0.25 and < 0.40)
        {
            // home will score
        }
           
        
        if (scoreProbability >= 0.64 && noScoreProbability is > 0.25 and < 0.38)
        {
            // beide treffen
        }
    }
}
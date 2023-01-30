using MathNet.Numerics.Distributions;

namespace AnalyseApp.Handlers;



public class TeamPerformance
{
    public int Shots { get; set; }
    public int ShotsOnGoal { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public double HalftimeGoalsScored { get; set; }
    public double HalftimeGoalsConceded { get; set; }
    public int HalftimeGoalMatches { get; set; }
    public int NoGoalMatches { get; set; }
    public int Offsides { get; set; }
    public int FoulsCommitted { get; set; }
    public int MatchesPlayed { get; set; }
    public int OneSideGoalMatches { get; set; }
    public int BothScoreMatches { get; set; }
    public int MoreThanTwoGoalsMatches { get; set; }
    public int TwoToThreeGoalMatches { get; set; }
    public double HalftimeGoals { get; set; }
    public int WinOneSideGoalMatches { get; set; }
    public int Win { get; set; }
    public int Loss { get; set; }
    public int Draw { get; set; }

    public double WinAccuracy()
    {
        var accuracy = (double)Win / MatchesPlayed;
        return accuracy;
    }
    
    public double LossAccuracy()
    {
        var accuracy = (double)Loss / MatchesPlayed;
        return accuracy;
    }
    
    public double DrawAccuracy()
    {
        var accuracy = (double)Draw / MatchesPlayed;
        return accuracy;
    }
    
    public double ShotsOnGoalAccuracy()
    {
        var accuracy = (double)ShotsOnGoal / MatchesPlayed;
        return accuracy;
    }

    public double ShotsOnGoalConversionRate()
    {
        var accuracy = (double)GoalsScored / MatchesPlayed;
        return accuracy;
    }

    public double ShotAccuracy()
    {
        var accuracy = (double)ShotsOnGoal / Shots;
        return accuracy;
    }

    public double OneSideGoalAccuracy()
    {
        var accuracy = (double)OneSideGoalMatches / MatchesPlayed;
        return accuracy;
    }
    
    public double BothScoreMatchesAccuracy()
    {
        var accuracy = (double)BothScoreMatches / MatchesPlayed;
        return accuracy;
    }
    
    
    public double MoreThanTwoGoalsMatchesAccuracy()
    {
        var accuracy = (double)MoreThanTwoGoalsMatches / MatchesPlayed;
        return accuracy;
    }
    
    public double TwoToThreeGoalMatchesAccuracy()
    {
        var accuracy = (double)TwoToThreeGoalMatches / MatchesPlayed;
        return accuracy;
    }

    public double ShotsPerAccuracy()
    {
        var accuracy = (double)Shots / MatchesPlayed;
        return accuracy;
    }

    public double OffsidesAccuracy()
    {
        var accuracy = (double)Offsides / MatchesPlayed;
        return accuracy;
    }

    public double FoulsCommittedAccuracy()
    {
        var accuracy = (double)FoulsCommitted / MatchesPlayed;
        return accuracy;
    }

    public double GoalsScoredAccuracy()
    {
        var accuracy = (double)GoalsScored / MatchesPlayed;
        return accuracy;
    }

    public double GoalsConcededAccuracy()
    {
        var accuracy = (double)GoalsConceded / MatchesPlayed;
        return accuracy;
    }

    public double NoGoalMatchAccuracy()
    {
        var accuracy = (double)NoGoalMatches / MatchesPlayed;
        return accuracy;
    }

    
    public double HalftimeGoalsScoredAccuracy()
    {
        var accuracy = (double)HalftimeGoalsScored / MatchesPlayed;
        return accuracy;
    }
    
    
    public double HalftimeGoalsConcededAccuracy()
    {
        var accuracy = (double)HalftimeGoalsConceded / MatchesPlayed;
        return accuracy;
    }
    
    
    public double HalftimeGoalMatchAccuracy()
    {
        var accuracy = (double)HalftimeGoalMatches / MatchesPlayed;
        return accuracy;
    }
    
    public double CleanSheetAccuracy()
    {
        var accuracy = (double)MatchesPlayed / (MatchesPlayed + GoalsConceded);
        return accuracy;
    }

    public double WinOneSideGoalMatchesAccuracy()
    {
        var accuracy = (double)WinOneSideGoalMatches / MatchesPlayed;
        return accuracy;
    }

    public double CompositeWin()
    {
        const double winWeight = 0.3;
        const double lossWeight = 0.2;
        const double drawWeight = 0.1;
        const double cleanSheetWeight = 0.2;
        const double winOneSideGoalWeight = 0.2;

        var winAccuracy = WinAccuracy();
        var lossAccuracy = LossAccuracy();
        var drawAccuracy = DrawAccuracy();
        var cleanSheetAccuracy= CleanSheetAccuracy();
        var winOneSideGoalMatchesAccuracy= WinOneSideGoalMatchesAccuracy();

        var compositeScore =
            winAccuracy * winWeight +
            lossAccuracy * lossWeight +
            drawAccuracy * drawWeight +
            cleanSheetAccuracy * cleanSheetWeight +
            winOneSideGoalMatchesAccuracy * winOneSideGoalWeight;

        return compositeScore;
    }
    
    public double CompositeMoreThanTwoGoals()
    {
        var compositeScore = MoreThanTwoGoalsMatchesAccuracy() * 0.5 +
                                    HalftimeGoalsConcededAccuracy() * 0.5; 

        return compositeScore;
    }
    
    public double CompositeScoreGoals()
    {
        var compositeScore = BothScoreMatchesAccuracy() * 0.4 +
                                   HalftimeGoalsScoredAccuracy() * 0.2 +
                                   GoalsConcededAccuracy() * 0.2 +
                                   GoalsScoredAccuracy() * 0.2; 

        return compositeScore;
    }
    
    public double CompositeDefense()
    {
        const double cleanSheet = 0.3;
        const double concededGoals = 0.3;
        const double shotsOnGoal = 0.2;
        const double halftimeConcededGoals = 0.2;
        
        var goalConcededAccuracy = GoalsConcededAccuracy();
        var halftimeGoalsConcededAccuracy = HalftimeGoalsConcededAccuracy();
        var cleanSheetAccuracy = CleanSheetAccuracy();
        var shotsOnGoalAccuracy = ShotAccuracy();

        var compositeScore =  goalConcededAccuracy * concededGoals +
                                    cleanSheetAccuracy * cleanSheet +
                                    shotsOnGoalAccuracy * shotsOnGoal + 
                                    halftimeGoalsConcededAccuracy * halftimeConcededGoals;

        return compositeScore;
    }

    public double CompositeOffsideAndFouls()
    {
        const double offSide = 0.6;
        const double foulCommitted = 0.4;
        
        var foulsCommittedAccuracy = FoulsCommittedAccuracy();
        var offsidesAccuracy = OffsidesAccuracy();

        var compositeScore = foulsCommittedAccuracy * foulCommitted + offsidesAccuracy * offSide;

        return compositeScore;
    }
    
    public double CompositeLessGoals()
    {
        const double noGoalWeight = 0.35;
        const double oneSideGoalsWeight = 0.35;
        const double goalConcededWeight = 0.15;
        const double halftimeConcededWeight = 0.15;

        var noGoalMatchAccuracy = NoGoalMatchAccuracy();
        var oneSideGoalAccuracy = OneSideGoalAccuracy();
        var goalConcededAccuracy = GoalsConcededAccuracy();
        var halftimeGoalsConcededAccuracy = HalftimeGoalsConcededAccuracy();

        var compositeScore = noGoalMatchAccuracy * noGoalWeight +
                             oneSideGoalAccuracy * oneSideGoalsWeight +
                             goalConcededAccuracy * goalConcededWeight + 
                             halftimeGoalsConcededAccuracy * halftimeConcededWeight
            ; 

        return compositeScore;
    }
    
    public double CompositeHalftimeScoredGames()
    {
        var halftimeGoalsScored = HalftimeGoalsScoredAccuracy();
        var halftimeGoalsConceded = HalftimeGoalsConcededAccuracy();
        var halftimeGoalMatchAccuracy = HalftimeGoalMatchAccuracy();

        var compositeScore = halftimeGoalsScored * 0.40 +
                             halftimeGoalsConceded * 0.30 +
                             halftimeGoalMatchAccuracy * 0.30 
            ; 

        return compositeScore;
    }
    
    
    public double ShotsProbability(int shots)
    {
        var poisson = new Poisson(ShotsPerAccuracy());
        return poisson.Probability(shots);
    }
    
    public double GoalsProbability(int goals)
    {
        var poisson = new Poisson(GoalsScoredAccuracy());
        return poisson.Probability(goals);
    }

    public double GoalsConcededProbability(int goals)
    {
        var poisson = new Poisson(GoalsConcededAccuracy());
        return poisson.Probability(goals);
    }

    public double NoGoalsProbability(int nogoals)
    {
        var poisson = new Poisson(NoGoalMatchAccuracy());
        return poisson.Probability(nogoals);
    }

    public double OffsidesProbability(int offsides)
    {
        var poisson = new Poisson(OffsidesAccuracy());
        return poisson.Probability(offsides);
    }
    
    public double FoulsCommittedProbability(int foul)
    {
        var poisson = new Poisson(FoulsCommittedAccuracy());
        return poisson.Probability(foul);
    }
}

namespace AnalyseApp.Models;

public record MatchAverage
{
    public double? AtLeastOneGoal { get; set; }
    public double? MoreThanTwoGoals { get; set; }
    public double? ZeroZero { get; set; }
    public double? TwoToThree { get; set; }
    public double? ScoreGoalsAverage { get; set; }
    public double? ScoreGoalsProbability { get; set; }
    public double? PoissonProbability { get; set; }
    public double? Win { get; set; }
    public double? Loss { get; set; }
    public double? Draw { get; set; }

    public override string ToString()
    {
        return $"""
                  At least one Goal: {AtLeastOneGoal},
                  More than two goals: {MoreThanTwoGoals},
                  Zero Zero games: {ZeroZero},
                 Two to three goals {TwoToThree},
                 ScoreGoalsAverage {ScoreGoalsAverage},
                 ScoreGoalsProbability {ScoreGoalsProbability},
                 PoissonProbability {PoissonProbability},
                 Win {Win},
                 Loss {Loss},
                 Draw {Draw}
                 """;
    }
}


public record Head2HeadAverage
{
    public int Count { get; set; }
    public double? BothTeamScore { get; set; }
    public double? MoreThanTwoGoals { get; set; }
    public double? TwoToThree { get; set; }
    public double? ZeroZero { get; set; }
    public double? ScoreGoalsAverage { get; set; }
    public double? ScoreGoalsProbability { get; set; }
    public double? PoissonProbability { get; set; }
    public double? HomWin { get; set; }
    public double? AwayWin { get; set; }
    public double? Draw { get; set; }
    
    public override string ToString()
    {
        return $"""
                 MatchCount: {Count},
                 BothTeamScore: {BothTeamScore},
                 More than two goals: {MoreThanTwoGoals}, 
                 Zero Zero games: {ZeroZero},
                 Two to three goals {TwoToThree},
                 ScoreGoalsAverage {ScoreGoalsAverage},
                 ScoreGoalsProbability {ScoreGoalsProbability},
                 PoissonProbability {PoissonProbability},
                 HomeWin {HomWin},
                 AwayWin {AwayWin},
                 Draw {Draw}
                 """;
    }
}


public record Match
{
    public required  DateTime Date { get; set; }
    public required string HomeTeam { get; set; }
    public required string AwayTeam { get; set; }
    public MatchAverage? HomeCurrentAverage { get; set; }
    public MatchAverage? AwayCurrentAverage { get; set; }
    public MatchAverage? HomeOverAllAverage { get; set; }
    public MatchAverage? AwayOverAllAverage { get; set; }
    public Head2HeadAverage? CurrentHeadToHeadAverage { get; set; }
    public Head2HeadAverage? HeadToHeadAverage { get; set; }
    
    public override string ToString() =>
        $"""
           Date: {Date}, HomeTeam: {HomeTeam}, AwayTeam: {AwayTeam}
           HomeCurrentAverage: {HomeCurrentAverage} AwayCurrentAverage: {AwayCurrentAverage} 
           HomeOverAllAverage: {HomeOverAllAverage} AwayOverAllAverage: {AwayOverAllAverage} 
           CurrentHeadToHeadAverage: {CurrentHeadToHeadAverage} HeadToHeadAverage: {HeadToHeadAverage}
          """;
}
namespace AnalyseApp.Extensions;

public static class PoissonServiceExtensions
{
    /// <summary>
    /// - This method calculates the probability of scoring a certain number of goals based on a given average (lambda),
    ///   summing up the probabilities for scoring 1 to 10 goals.
    /// </summary>
    /// <param name="lambda"></param>
    /// <returns></returns>
    public static double GetScoredGoalProbabilityBy(this double lambda)
    {
        var probability = 0.0;
        for (var k = 1; k < 10; k++)
        {
            probability += Math.Pow(lambda, k) * Math.Exp(-lambda) / Factorial(k);
        }
        
        return probability;
    }
    
    private static double Factorial(int n)
    {
        if (n <= 1) return 1;
        return n * Factorial(n - 1);
    }
}
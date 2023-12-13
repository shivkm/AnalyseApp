namespace AnalyseApp.Extensions;

public static class PoissonExtensions
{
    /// <summary>
    /// - This method calculates the probability of scoring a certain number of goals based on a given average (lambda),
    ///   summing up the probabilities for scoring 1 to 10 goals.
    /// </summary>
    /// <param name="lambda"></param>
    /// <returns></returns>
    public static double PoissonProbability(this float lambda, int k)
    {
        return Math.Pow(lambda, k) * Math.Exp(-lambda) / Factorial(k);
    }

    private static double Factorial(int n)
    {
        double result = 1;
        for (var i = 1; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }

}
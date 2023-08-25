namespace AnalyseApp.Extensions;

public static class PoissonServiceExtensions
{
    public static double GetScoredGoalProbabilityBy(this double lambda)
    {
        var score = new List<int> { 1, 2, 3, 4 };
        var probability = 0.0;

        score.ForEach(i => { probability += PoissonProbability(lambda, i); });
        
        return probability;
    }
    
    private static double PoissonProbability(double lambda, int k)
    {
        // Calculate the Poisson probability for k goals given a lambda value
        return Math.Pow(lambda, k) * Math.Exp(-lambda) / Factorial(k);
    }
    
    private static double Factorial(int n)
    {
        if (n <= 1) return 1;
        return n * Factorial(n - 1);
    }
}
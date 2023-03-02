using System.Numerics;

namespace PredictionTool.Extensions;

public static class MathExtensions
{
    internal static T Divide<T>(this T left, T right) where T: INumber<T> => left / right;
    
    internal static double Divide(this int left, int right)
    {
        var value = (double)left / right;
        var result = double.IsNaN(value) ? 0.0 : value;

        return Math.Round(result, 2);
    }
    
    internal static double CalculateWeighting(this double left, double right, double leftWeight = 0.35)
    {
        var rightWeight = 1.0 - leftWeight;
        var result = left * leftWeight + right * rightWeight;
        return Math.Round(result, 2);
    }
}

public static class RandomExtensions
{
    public static double NextGaussian(this Random random, double mean, double standardDeviation)
    {
        var u1 = 1.0 - random.NextDouble();
        var u2 = 1.0 - random.NextDouble();
        var standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + standardDeviation * standardNormal;
    }
}
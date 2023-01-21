using System.Numerics;

namespace AnalyseApp.Extensions;

public static class MathExtensions
{
    internal static T Divide<T>(this T left, T right) where T: INumber<T> => left / right;
    
    internal static double CalculateWeighting(this double left, double right, double leftWeight = 0.35)
    {
        var result = left * leftWeight + right * (100 - leftWeight);
        return Math.Round(result, 2);
    }
}
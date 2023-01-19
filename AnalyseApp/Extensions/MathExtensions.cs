using System.Numerics;

namespace AnalyseApp.Extensions;

public static class MathExtensions
{
    internal static T Divide<T>(this T left, T right) where T: INumber<T> => left / right;
    
    internal static double CalculateWeighting(this double left, double right)
    {
        var result = left * 0.35 + right * 0.65;
        return Math.Round(result, 2);
    }
}
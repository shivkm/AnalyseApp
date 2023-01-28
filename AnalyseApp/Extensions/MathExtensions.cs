using System.Numerics;

namespace AnalyseApp.Extensions;

public static class MathExtensions
{
    internal static T Divide<T>(this T left, T right) where T: INumber<T> => left / right;
    
    internal static double CalculateWeighting(this double left, double right, double leftWeight = 0.35)
    {
        var rightWeight = 1 - leftWeight;
        var result = left * leftWeight + right * rightWeight;
        return Math.Round(result, 2);
    }
    
    internal static double GetPercent<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var found = 0;
        var total = 0;

        var enumerable = source.ToList();
        if (!enumerable.Any())
            return 0;

        foreach (var item in enumerable)
        {
            ++total;
            if (predicate(item))
            {
                found += 1;
            }
        }

        var value =  (double)found / total;

        return Math.Round(value, 2);
    }
}
using System.Globalization;

namespace AnalyseApp.Extensions;

public static class MathExtensions
{
    internal static double Divide(this int left, int right)
    {
        var value = (double)left / right;
        var result = double.IsNaN(value) ? 0.0 : value;

        return Math.Round(result, 2);
    }
    
    
    internal static double ToDouble(this double value)
    {
        var formattedValue = value.ToString().Insert(1, ",");
        double.TryParse(formattedValue.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture,
            out var parsedValue);

        return parsedValue;
    }
}
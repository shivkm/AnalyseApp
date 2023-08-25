namespace AnalyseApp.Extensions;

public static class NormalDistribution
{
    public static double CumulativeDistribution(double x, double mean, double standardDeviation)
    {
        var z = (x - mean) / standardDeviation;
        var cumulativeProbability = 0.5 * (1 + MathErf(z / Math.Sqrt(2)));

        return cumulativeProbability;
    }

    private static double MathErf(double x)
    {
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        double sign = Math.Sign(x);
        x = Math.Abs(x);

        var t = 1.0 / (1.0 + p * x);
        var y = ((((a5 * t + a4) * t + a3) * t + a2) * t + a1) * t;

        return sign * (1 - y * Math.Exp(-x * x));
    }
}

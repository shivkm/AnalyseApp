using System.Globalization;

namespace AnalyseApp.Extensions;

public static class DateTimeExtensions
{
    public static DateTime Parse(this string datetime)
    {
        if (DateTime.TryParseExact(datetime, "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ||
            DateTime.TryParseExact(datetime, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            return parsedDate;
        }
        return DateTime.MinValue;
    }
}
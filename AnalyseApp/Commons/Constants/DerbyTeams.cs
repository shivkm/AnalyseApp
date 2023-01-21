namespace AnalyseApp.Commons.Constants;

public static class DerbyTeams
{
    internal static List<string> GetDerbyMatches()
    {
        var result = new List<string>
        {
            "Man United:Man City",
            "Man City:Man United",
            "Arsenal:Tottenham",
            "Tottenham:Arsenal",
            "Liverpool:Everton",
            "Everton:Liverpool",
            "Chelsea:Fullham",
            "Fullham:Chelsea",
            "Chelsea:Tottenham",
            "Tottenham:Chelsea",
            "Arsenal:West Ham",
            "West Ham:Arsenal",
            "Crystal Palace:Brighton",
            "Brighton:Crystal Palace"
        };

        return result;
    }
}
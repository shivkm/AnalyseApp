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
            "Brighton:Crystal Palace",
            "Dortmund:Schalke 04",
            "Schalke 04:Dortmund",
            "Hertha:Union Berlin",
            "Union Berlin:Hertha",
            "Milan:Inter",
            "Inter:Milan",
            "Juventus:Turin",
            "Turin:Juventus",
            "Lazio:Roma",
            "Roma:Lazio",
            "Napoli:Roma",
            "Roma:Napoli",
            "Nice:Monaco",
            "Monaco:Nice",
            "Lille:Lens",
            "Lens:Lille"
            
        };

        return result;
    }
    internal static List<string> PopularTeams()
    {
        var result = new List<string>
        {
            "Man City",
            "Liverpool",
            "Union Berlin",
            "Paris SG",
        };

        return result;
    }
}
namespace AnalyseApp.Interfaces;

public interface IPredictService
{
    bool OverUnderPredictor(string home, string away);

    string OverUnderPredictionBy(string home, string away, string playedOn);

    bool TeamAnalysisBy();
}
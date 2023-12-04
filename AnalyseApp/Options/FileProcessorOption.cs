namespace AnalyseApp.Options;

public class FileProcessorOptions
{
    public string Upcoming { get; init; } = default!;
    public string RawCsvDir { get; init; } = default!;
    public string MachineLearningModel { get; init; } = default!;
}
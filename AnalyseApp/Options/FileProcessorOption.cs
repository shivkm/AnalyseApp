namespace AnalyseApp.Options;

public class FileProcessorOptions
{
    public string Upcoming { get; set; } = default!;
    public string Historical { get; set; } = default!;
    public string RawCsvDir { get; set; } = default!;
    public string FilesDir { get; set; } = default!;
    public string AnalyseResult { get; set; } = default!;
    public string MachineLearning { get; set; } = default!;
}
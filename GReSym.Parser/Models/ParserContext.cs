using GReSym.Parser.Enums;

namespace GReSym.Parser.Models;

public class ParserContext
{
    public string Source { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public ParserMode Mode { get; set; } = ParserMode.Full;
    public List<string> Identifiers { get; set; } = new();
    public int MaxDegreeOfParallelism { get; set; } = 5;
    public int BatchSize { get; set; } = 10;
    public int DelayBetweenBatchesMs { get; set; } = 1000;
    public bool AutoSaveCheckpoint { get; set; } = true;
    public string CheckpointName { get; set; } = "default";
    public Dictionary<string, object> Parameters { get; set; } = new();
}
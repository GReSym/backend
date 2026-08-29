namespace GReSym.Parser.Models;

public class ParsingProgressEventArgs : EventArgs
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public double ProgressPercentage => TotalItems > 0 ? ProcessedItems * 100.0 / TotalItems : 0;
    public string CurrentSource { get; set; } = string.Empty;
    public string CurrentOperation { get; set; } = string.Empty;
    public ParsingResult? ParsingResult { get; set; }
}
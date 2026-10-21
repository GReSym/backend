namespace GReSym.Parser.Models;

public class ParsingProgressEventArgs : EventArgs
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    // Source identifiers (game ids) of this run. Differs from items for reviews: one game → many reviews
    public int TotalIdentifiers { get; set; }
    public int ProcessedIdentifiers { get; set; }
    public double ProgressPercentage => TotalItems > 0 ? ProcessedItems * 100.0 / TotalItems : 0;
    public string CurrentSource { get; set; } = string.Empty;
    public string CurrentOperation { get; set; } = string.Empty;
    public ParsingResult? ParsingResult { get; set; }
}
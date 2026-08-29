using Newtonsoft.Json;
using GReSym.Parser.Enums;

namespace GReSym.Parser.Models;

[JsonObject]
public class ParserState
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Source { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; }
    
    // Прогресс
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int FailedItems { get; set; }
    public int SkippedItems { get; set; }
    public List<string> ProcessedIdentifiers { get; set; } = new();
    public List<string> PendingIdentifiers { get; set; } = new();
    public List<string> FailedIdentifiers { get; set; } = new();
    public List<string> SkippedIdentifiers { get; set; } = new();
    
    // Статистика
    public Dictionary<string, int> Statistics { get; set; } = new()
    {
        ["games_added"] = 0,
        ["games_updated"] = 0,
        ["reviews_added"] = 0,
        ["errors"] = 0
    };
    
    public ParserStatus Status { get; set; } = ParserStatus.Idle;
    public string? CurrentCheckpoint { get; set; }
}
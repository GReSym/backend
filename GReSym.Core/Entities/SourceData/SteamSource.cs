using GReSym.Core.Entities.Base;
using GReSym.Core.Enums;
using GReSym.Core.ValueObjects;
using GReSym.Core.Entities.GameInfo;

namespace GReSym.Core.Entities.SourceData;

public class SteamSource : BaseEntity
{
    public int? GameId { get; set; }
    public string SteamAppId { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int? RecommendationsCount { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public bool? IsFree { get; set; }
    
    // Навигационные свойства
    public virtual Game? Game { get; set; }
}
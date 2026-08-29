using GReSym.Core.Entities.Base;
using GReSym.Core.Enums;

namespace GReSym.Core.Entities.GameInfo;

public class Screenshot : BaseEntity
{
    public int GameId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Caption { get; set; }
    public ScreenshotSource Source { get; set; }
    
    // Навигационные свойства
    public virtual Game Game { get; set; } = null!;
}
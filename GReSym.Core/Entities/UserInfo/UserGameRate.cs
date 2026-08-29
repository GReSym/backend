using GReSym.Core.Entities.GameInfo;

namespace GReSym.Core.Entities.UserInfo;

public class UserGameRate
{
    public int UserId { get; set; }
    public int GameId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public int? PlayerHours { get; set; }
    public bool? Recommend { get; set; }

    // Навигационные свойства
    public virtual User User { get; set; } = null!;
    public virtual Game Game { get; set; } = null!;
}
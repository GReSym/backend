namespace GReSym.Core.Entities.GameInfo;

public class GameTag
{
    public int GameId { get; set; }
    public int TagId { get; set; }
    
    // Навигационные свойства
    public virtual Game Game { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}
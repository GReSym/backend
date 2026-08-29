using GReSym.Core.Entities.Base;
using GReSym.Core.Entities.Feedback;
using GReSym.Core.Entities.SourceData;
using GReSym.Core.Entities.UserInfo;
using System.Collections.ObjectModel;

namespace GReSym.Core.Entities.GameInfo;

public class Game : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly ReleaseDate { get; set; }
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string? HeaderImageUrl { get; set; }
    
    // Навигационные свойства
    public virtual ICollection<GameTag> GameTags { get; set; } = new Collection<GameTag>();
    public virtual ICollection<Screenshot> Screenshots { get; set; } = new Collection<Screenshot>();
    public virtual ICollection<Review> Reviews { get; set; } = new Collection<Review>();
    public virtual ICollection<UserGameRate> UserGameRates { get; set; } = new Collection<UserGameRate>();
    public virtual SteamSource? SteamSource { get; set; }
    public virtual MetacriticSource? MetacriticSource { get; set; }
    
    // Вычисляемые свойства (только для чтения)
    public ICollection<Tag> Tags => GameTags.Select(gt => gt.Tag).ToList();
}
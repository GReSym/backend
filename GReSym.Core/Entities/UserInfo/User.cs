using GReSym.Core.Entities.Base;
using System.Collections.ObjectModel;

namespace GReSym.Core.Entities.UserInfo;

public class User : AuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    
    // Навигационные свойства
    public virtual ICollection<UserGameRate> UserGameRates { get; set; } = new Collection<UserGameRate>();
}
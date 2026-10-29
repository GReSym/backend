using GReSym.Core.Entities.UserInfo;

namespace GReSym.Core.Interfaces;

public interface IUserSettingsRepository
{
    /// <summary>Tracked row, null if the user never changed their settings.</summary>
    Task<UserSettings?> GetAsync(int userId);

    Task AddAsync(UserSettings settings);
}

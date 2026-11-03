using GReSym.Core.Entities.Settings;

namespace GReSym.Core.Interfaces;

public interface IAppSettingsRepository
{
    /// <summary>Tracked row, null if the setting was never changed.</summary>
    Task<AppSetting?> GetAsync(string key);

    Task AddAsync(AppSetting setting);
}

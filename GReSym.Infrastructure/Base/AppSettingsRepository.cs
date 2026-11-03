using GReSym.Core.Entities.Settings;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly DbSet<AppSetting> _dbSet;

    public AppSettingsRepository(ApplicationDbContext context)
    {
        _dbSet = context.Set<AppSetting>();
    }

    public async Task<AppSetting?> GetAsync(string key)
    {
        return await _dbSet.FindAsync(key);
    }

    public async Task AddAsync(AppSetting setting)
    {
        await _dbSet.AddAsync(setting);
    }
}

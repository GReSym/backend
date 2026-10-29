using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class UserSettingsRepository : IUserSettingsRepository
{
    private readonly DbSet<UserSettings> _dbSet;

    public UserSettingsRepository(ApplicationDbContext context)
    {
        _dbSet = context.Set<UserSettings>();
    }

    public async Task<UserSettings?> GetAsync(int userId)
    {
        return await _dbSet.FindAsync(userId);
    }

    public async Task AddAsync(UserSettings settings)
    {
        await _dbSet.AddAsync(settings);
    }
}

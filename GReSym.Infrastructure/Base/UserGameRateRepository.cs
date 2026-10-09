using GReSym.Core.Common;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class UserGameRateRepository : IUserGameRateRepository
{
    private readonly DbSet<UserGameRate> _dbSet;

    public UserGameRateRepository(ApplicationDbContext context)
    {
        _dbSet = context.Set<UserGameRate>();
    }

    public async Task<UserGameRate?> GetAsync(int userId, int gameId)
    {
        return await _dbSet.FindAsync(userId, gameId);
    }

    public async Task<IEnumerable<UserGameRate>> GetByUserAsync(int userId, int page = 1, int pageSize = Constants.Api.DefaultPageSize)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(r => r.Game)
                .ThenInclude(g => g.GameTags)
                    .ThenInclude(gt => gt.Tag)
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.GameId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserGameRate>> GetAllByUserAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task AddAsync(UserGameRate rate)
    {
        await _dbSet.AddAsync(rate);
    }

    public void Remove(UserGameRate rate)
    {
        _dbSet.Remove(rate);
    }
}

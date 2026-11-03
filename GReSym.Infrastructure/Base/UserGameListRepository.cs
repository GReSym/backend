using GReSym.Core.Common;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Enums;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class UserGameListRepository : IUserGameListRepository
{
    private readonly DbSet<UserGameListEntry> _dbSet;

    public UserGameListRepository(ApplicationDbContext context)
    {
        _dbSet = context.Set<UserGameListEntry>();
    }

    public async Task<UserGameListEntry?> GetAsync(int userId, int gameId)
    {
        return await _dbSet.FindAsync(userId, gameId);
    }

    public async Task<List<UserGameListEntry>> GetPageAsync(int userId, GameListStatus status, int page = 1,
        int pageSize = Constants.Api.DefaultPageSize)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Status == status)
            .Include(e => e.Game)
                .ThenInclude(g => g.GameTags)
                    .ThenInclude(gt => gt.Tag)
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.GameId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<UserGameListEntry>> GetAllByUserAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync();
    }

    public async Task AddAsync(UserGameListEntry entry)
    {
        await _dbSet.AddAsync(entry);
    }

    public void Remove(UserGameListEntry entry)
    {
        _dbSet.Remove(entry);
    }
}

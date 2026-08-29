using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class GameTagRepository : RepositoryBase<GameTag>, IGameTagRepository
{
    public GameTagRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAsync(int gameId, int tagId)
    {
        return await _dbSet
            .AnyAsync(gt => gt.GameId == gameId && gt.TagId == tagId);
    }

    public async Task RemoveByGameAndTagAsync(int gameId, int tagId)
    {
        var gameTag = await _dbSet
            .FirstOrDefaultAsync(gt => gt.GameId == gameId && gt.TagId == tagId);

        if (gameTag != null)
        {
            _dbSet.Remove(gameTag);
        }
    }

    public async Task<IEnumerable<Tag>> GetTagsByGameIdAsync(int gameId)
    {
        return await _dbSet
            .Where(gt => gt.GameId == gameId)
            .Include(gt => gt.Tag)
            .Select(gt => gt.Tag)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetGamesByTagIdAsync(int tagId, int page = 1, int pageSize = 50)
    {
        return await _dbSet
            .Where(gt => gt.TagId == tagId)
            .Include(gt => gt.Game)
            .Select(gt => gt.Game)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetGameCountByTagAsync(int tagId)
    {
        return await _dbSet
            .CountAsync(gt => gt.TagId == tagId);
    }
}
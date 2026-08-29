using GReSym.Core.Common;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class GameRepository : RepositoryBase<Game>, IGameRepository
{
    public GameRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Game?> GetByTitleAsync(string title)
    {
        return await _dbSet
            .FirstOrDefaultAsync(g => g.Title == title);
    }

    public async Task<Game?> GetBySteamAppIdAsync(string steamAppId)
    {
        return await _dbSet
            .Include(g => g.SteamSource)
            .FirstOrDefaultAsync(g => g.SteamSource != null && g.SteamSource.SteamAppId == steamAppId);
    }

    public async Task<IEnumerable<Game>> SearchAsync(
        string searchTerm,
        int page = 1,
        int pageSize = Constants.Api.DefaultPageSize,
        List<int>? tagIds = null)
    {
        var query = _dbSet
            .Include(g => g.GameTags)
            .ThenInclude(gt => gt.Tag)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(g =>
                g.Title.Contains(searchTerm) ||
                g.Description.Contains(searchTerm));
        }

        if (tagIds != null && tagIds.Count > 0)
        {
            query = query.Where(g =>
                tagIds.All(tid => g.GameTags.Any(gt => gt.TagId == tid)));
        }

        return await query
            .OrderBy(g => g.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetPopularGamesAsync(int page = 1, int pageSize = Constants.Api.DefaultPageSize, List<int>? tagIds = null)
    {
        var query = _dbSet
            .Include(g => g.GameTags)
            .ThenInclude(gt => gt.Tag)
            .AsQueryable();
        
        if (tagIds != null && tagIds.Count > 0)
        {
            query = query.Where(g =>
                tagIds.All(tid => g.GameTags.Any(gt => gt.TagId == tid)));
        }

        return await query
            .OrderBy(g => g.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetGamesAsync(int page = 1, int pageSize = Constants.Api.DefaultPageSize, List<int>? tagIds = null)
    {
        var query = _dbSet
            .Include(g => g.GameTags)
            .ThenInclude(gt => gt.Tag)
            .AsQueryable();

        if (tagIds != null && tagIds.Count > 0)
        {
            query = query.Where(g =>
                tagIds.All(tid => g.GameTags.Any(gt => gt.TagId == tid)));
        }

        return await query
            .OrderBy(g => g.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> ExistsBySteamAppIdAsync(string steamAppId)
    {
        return await _dbSet
            .Include(g => g.SteamSource)
            .AnyAsync(g => g.SteamSource != null && g.SteamSource.SteamAppId == steamAppId);
    }

    public async Task<bool> ExistsByTitleAndDeveloperAsync(string title, string developer)
    {
        return await _dbSet
            .AnyAsync(g => g.Title == title && g.Developer == developer);
    }

    // Override для включения связанных данных
    public override async Task<Game?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(g => g.GameTags)
                .ThenInclude(gt => gt.Tag)
            .Include(g => g.Screenshots)
            .Include(g => g.SteamSource)
            .Include(g => g.MetacriticSource)
            .FirstOrDefaultAsync(g => g.Id == id);
    }
}
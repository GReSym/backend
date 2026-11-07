using GReSym.Core.Common;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Entities.SourceData;
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
            .Include(g => g.SteamSource)
            .FirstOrDefaultAsync(g => g.Title == title);
    }

    public async Task<Game?> GetBySteamAppIdAsync(int steamAppId)
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

    public async Task<IEnumerable<Game>> GetPopularGamesAsync(
        int page = 1,
        int pageSize = Constants.Api.DefaultPageSize,
        List<int>? tagIds = null,
        IReadOnlyCollection<int>? excludeIds = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (tagIds != null && tagIds.Count > 0)
        {
            query = query.Where(g =>
                tagIds.All(tid => g.GameTags.Any(gt => gt.TagId == tid)));
        }

        if (excludeIds != null && excludeIds.Count > 0)
        {
            query = query.Where(g => !excludeIds.Contains(g.Id));
        }

        // Two steps: sorting only ids takes ~50 ms, while sorting full rows (with ~2 KB descriptions)
        // takes ~3.5 s on 67k games. NULL counts sort last in descending order.
        var pageIds = await query
            .OrderByDescending(g => g.SteamSource!.RecommendationsCount)
            .ThenBy(g => g.Id)
            .Select(g => g.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var games = (await GetByIdsAsync(pageIds)).ToDictionary(g => g.Id);

        return pageIds.Where(games.ContainsKey).Select(id => games[id]).ToList();
    }

    public async Task<IEnumerable<Game>> GetByIdsAsync(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
            return [];

        return await _dbSet
            .Include(g => g.GameTags)
            .ThenInclude(gt => gt.Tag)
            .Where(g => ids.Contains(g.Id))
            .ToListAsync();
    }

    public async Task<List<int>> FilterIdsByTagsAsync(IReadOnlyCollection<int> ids, IReadOnlyCollection<int> tagIds)
    {
        if (ids.Count == 0)
            return [];

        return await _dbSet
            .Where(g => ids.Contains(g.Id))
            .Where(g => tagIds.All(tid => g.GameTags.Any(gt => gt.TagId == tid)))
            .Select(g => g.Id)
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

    public async Task<List<int>> GetIdsWithExternalImagesAsync(int count, bool withScreenshots,
        IReadOnlyCollection<int>? ids, IReadOnlyCollection<int> excludeIds, CancellationToken cancellationToken = default)
    {
        // StartsWith → LIKE 'http%' (MediaPaths.IsExternal: every external reference is an http(s) URL)
        var query = withScreenshots
            ? _dbSet.Where(g => g.HeaderImageUrl!.StartsWith("http") || g.Screenshots.Any(s => s.Url.StartsWith("http")))
            : _dbSet.Where(g => g.HeaderImageUrl!.StartsWith("http"));

        if (ids != null)
            query = query.Where(g => ids.Contains(g.Id));
        if (excludeIds.Count > 0)
            query = query.Where(g => !excludeIds.Contains(g.Id));

        return await query
            .OrderByDescending(g => g.SteamSource!.RecommendationsCount)
            .ThenBy(g => g.Id)
            .Select(g => g.Id)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Game>> GetWithScreenshotsByIdsAsync(IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        return await _dbSet
            .Include(g => g.Screenshots)
            .Where(g => ids.Contains(g.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<MediaCounts> CountMediaAsync(CancellationToken cancellationToken = default)
    {
        var headers = await _dbSet.CountAsync(g => g.HeaderImageUrl != null && g.HeaderImageUrl != "", cancellationToken);
        var externalHeaders = await _dbSet.CountAsync(g => g.HeaderImageUrl!.StartsWith("http"), cancellationToken);
        var screenshots = _context.Set<Screenshot>();
        var screenshotsTotal = await screenshots.CountAsync(cancellationToken);
        var externalScreenshots = await screenshots.CountAsync(s => s.Url.StartsWith("http"), cancellationToken);

        return new MediaCounts(headers, externalHeaders, screenshotsTotal, externalScreenshots);
    }

    public async Task<bool> ExistsBySteamAppIdAsync(int steamAppId)
    {
        return await _dbSet
            .Include(g => g.SteamSource)
            .AnyAsync(g => g.SteamSource != null && g.SteamSource.SteamAppId == steamAppId);
    }

    public async Task<HashSet<int>> GetExistingSteamAppIdsAsync(int minAppId, int maxAppId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SteamSource>()
            .AsNoTracking()
            .Where(s => s.SteamAppId >= minAppId && s.SteamAppId <= maxAppId)
            .Select(s => s.SteamAppId)
            .ToHashSetAsync(cancellationToken);
    }

    public async Task<HashSet<int>> GetGameIdsWithReviewsAsync(int minGameId, int maxGameId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<GReSym.Core.Entities.Feedback.Review>()
            .AsNoTracking()
            .Join(_dbSet.AsNoTracking(), review => review.GameId, game => game.Id, (review, game) => game.Id)
            .Where(gameId => gameId >= minGameId && gameId <= maxGameId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);
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
            .FirstOrDefaultAsync(g => g.Id == id);
    }
}
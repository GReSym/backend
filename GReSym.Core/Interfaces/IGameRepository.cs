using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Common;

namespace GReSym.Core.Interfaces;

public interface IGameRepository : IRepository<Game>
{
    // Базовые операции
    Task<Game?> GetByTitleAsync(string title);
    Task<Game?> GetBySteamAppIdAsync(int steamAppId);

    // Поиск и фильтрация
    Task<IEnumerable<Game>> SearchAsync(string searchTerm, int page = 1, int pageSize = Constants.Api.DefaultPageSize, List<int>? tagsIds = null);
    /// <summary>Ordered by Steam recommendations count (games without it last).</summary>
    Task<IEnumerable<Game>> GetPopularGamesAsync(int page = 1, int pageSize = Constants.Api.DefaultPageSize, List<int>? tagIds = null, IReadOnlyCollection<int>? excludeIds = null);
    /// <summary>Games with tags, in no particular order. Unknown ids are skipped.</summary>
    Task<IEnumerable<Game>> GetByIdsAsync(IReadOnlyCollection<int> ids);
    /// <summary>Those of <paramref name="ids"/> that have all <paramref name="tagIds"/>, in no particular order.</summary>
    Task<List<int>> FilterIdsByTagsAsync(IReadOnlyCollection<int> ids, IReadOnlyCollection<int> tagIds);
    Task<IEnumerable<Game>> GetGamesAsync(int page = 1, int pageSize = Constants.Api.DefaultPageSize, List<int>? tagsIds = null);

    // Media migration (Steam CDN → media store, see MediaPaths)
    /// <summary>
    /// Ids of games whose header (or, with <paramref name="withScreenshots"/>, a screenshot) is still an external URL,
    /// most popular first. Only <paramref name="ids"/> when given.
    /// </summary>
    Task<List<int>> GetIdsWithExternalImagesAsync(int count, bool withScreenshots, IReadOnlyCollection<int>? ids,
        IReadOnlyCollection<int> excludeIds, CancellationToken cancellationToken = default);
    /// <summary>Tracked games with their screenshots, in no particular order.</summary>
    Task<List<Game>> GetWithScreenshotsByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    Task<MediaCounts> CountMediaAsync(CancellationToken cancellationToken = default);

    // Для парсинга
    Task<bool> ExistsBySteamAppIdAsync(int steamAppId);
    Task<bool> ExistsByTitleAndDeveloperAsync(string title, string developer);
}

/// <summary>How many image references are external (Steam CDN) and how many there are in total.</summary>
public sealed record MediaCounts(int Headers, int ExternalHeaders, int Screenshots, int ExternalScreenshots);

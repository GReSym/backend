using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Common;

namespace GReSym.Core.Interfaces;

public interface IGameRepository : IRepository<Game>
{
    // Базовые операции
    Task<Game?> GetByTitleAsync(string title);
    Task<Game?> GetBySteamAppIdAsync(string steamAppId);

    // Поиск и фильтрация
    Task<IEnumerable<Game>> SearchAsync(string searchTerm, int page = 1, int pageSize = Constants.Api.DefaultPageSize, List<int>? tagsIds = null);
    Task<IEnumerable<Game>> GetPopularGamesAsync(int page = 1, int pageSize = Constants.Api.DefaultPageSize, List<int>? tagIds = null);
    Task<IEnumerable<Game>> GetGamesAsync(int page = 1, int pageSize = Constants.Api.DefaultPageSize, List<int>? tagsIds = null);

    // Для парсинга
    Task<bool> ExistsBySteamAppIdAsync(string steamAppId);
    Task<bool> ExistsByTitleAndDeveloperAsync(string title, string developer);
}
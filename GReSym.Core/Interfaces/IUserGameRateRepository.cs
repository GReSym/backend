using GReSym.Core.Common;
using GReSym.Core.Entities.UserInfo;

namespace GReSym.Core.Interfaces;

public interface IUserGameRateRepository
{
    Task<UserGameRate?> GetAsync(int userId, int gameId);

    /// <summary>A page of the user's ratings with games (and their tags), ordered by game id.</summary>
    /// <param name="minHours">only entries with more than this many hours played</param>
    /// <param name="unevaluated">only entries without a recommend value</param>
    Task<IEnumerable<UserGameRate>> GetByUserAsync(int userId, int page = 1, int pageSize = Constants.Api.DefaultPageSize,
        int? minHours = null, bool unevaluated = false);

    /// <summary>All ratings of the user without navigation properties (for recommendations).</summary>
    Task<IEnumerable<UserGameRate>> GetAllByUserAsync(int userId);

    /// <summary>All ratings of the user with game tags (for the profile stats).</summary>
    Task<IEnumerable<UserGameRate>> GetAllByUserWithTagsAsync(int userId);

    Task AddAsync(UserGameRate rate);
    void Remove(UserGameRate rate);
}

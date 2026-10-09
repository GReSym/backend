using GReSym.Core.Common;
using GReSym.Core.Entities.UserInfo;

namespace GReSym.Core.Interfaces;

public interface IUserGameRateRepository
{
    Task<UserGameRate?> GetAsync(int userId, int gameId);

    /// <summary>A page of the user's ratings with games (and their tags), ordered by game id.</summary>
    Task<IEnumerable<UserGameRate>> GetByUserAsync(int userId, int page = 1, int pageSize = Constants.Api.DefaultPageSize);

    /// <summary>All ratings of the user without navigation properties (for recommendations).</summary>
    Task<IEnumerable<UserGameRate>> GetAllByUserAsync(int userId);

    Task AddAsync(UserGameRate rate);
    void Remove(UserGameRate rate);
}

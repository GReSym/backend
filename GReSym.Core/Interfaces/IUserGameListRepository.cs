using GReSym.Core.Common;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Enums;

namespace GReSym.Core.Interfaces;

/// <summary>The users' planned / rejected games. Shares the scoped DbContext: <see cref="IUnitOfWork"/> saves the changes.</summary>
public interface IUserGameListRepository
{
    /// <summary>Tracked entry, or null if the game isn't listed.</summary>
    Task<UserGameListEntry?> GetAsync(int userId, int gameId);

    /// <summary>A page of one list with games (and their tags), the most recently listed first.</summary>
    Task<List<UserGameListEntry>> GetPageAsync(int userId, GameListStatus status, int page = 1,
        int pageSize = Constants.Api.DefaultPageSize);

    /// <summary>All entries of the user without navigation properties (for recommendations).</summary>
    Task<List<UserGameListEntry>> GetAllByUserAsync(int userId);

    Task AddAsync(UserGameListEntry entry);
    void Remove(UserGameListEntry entry);
}

using GReSym.Application.DTO.GameLists;

namespace GReSym.Application.Interfaces;

/// <summary>The user's planned / rejected games (see <see cref="GReSym.Core.Entities.UserInfo.UserGameListEntry"/>).</summary>
public interface IGameListsService
{
    Task<GameListPageDto> List(int userId, GameListPageRequestDto request);
    Task<GameListEntryDto> Get(int userId, int gameId);
    Task<GameListEntryDto> Set(int userId, int gameId, string status);
    Task Remove(int userId, int gameId);
}

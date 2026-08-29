using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Common;

namespace GReSym.Core.Interfaces;

public interface IGameTagRepository : IRepository<GameTag>
{
    Task<bool> ExistsAsync(int gameId, int tagId);
    Task RemoveByGameAndTagAsync(int gameId, int tagId);
    Task<IEnumerable<Tag>> GetTagsByGameIdAsync(int gameId);
    Task<IEnumerable<Game>> GetGamesByTagIdAsync(int tagId, int page = 1, int pageSize = Constants.Api.DefaultPageSize);
    Task<int> GetGameCountByTagAsync(int tagId);
}
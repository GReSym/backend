using GReSym.Core.Common;
using GReSym.Core.Entities.Feedback;
using GReSym.Core.Enums;

namespace GReSym.Core.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    Task<IEnumerable<Review>> GetByGameIdAsync(int gameId, int page = 1, int pageSize = Constants.Api.DefaultPageSize);
    Task<IEnumerable<Review>> GetBySourceAsync(ReviewSource source, int page = 1, int pageSize = Constants.Api.DefaultPageSize);
    Task<int> GetCountByGameAsync(int gameId);
    Task UpsertAsync(Review review);
}
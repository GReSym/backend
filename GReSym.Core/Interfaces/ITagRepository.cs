using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Common;

namespace GReSym.Core.Interfaces;


public interface ITagRepository : IRepository<Tag>
{
    Task<Tag?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
    Task<IEnumerable<Tag>> SearchByNameAsync(string searchTerm, int limit = Constants.Api.DefaultPageSize);
    Task<Tag> GetOrCreateAsync(string name, CancellationToken cancellationToken = default);
}
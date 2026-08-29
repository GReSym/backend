using GReSym.Parser.Models;

namespace GReSym.Parser.Interfaces;

public interface IDataLoader<T>
{
    Task<LoadResult> LoadAsync(T data, CancellationToken cancellationToken);
    Task<BatchLoadResult> LoadBatchAsync(IEnumerable<T> data, CancellationToken cancellationToken);
}
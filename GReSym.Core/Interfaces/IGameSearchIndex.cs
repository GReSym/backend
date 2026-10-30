namespace GReSym.Core.Interfaces;

/// <summary>Relevance-ranked game search of the ML service (BM25 + embeddings, see ml.md "Search").</summary>
public interface IGameSearchIndex
{
    /// <summary>Game ids, most relevant first.</summary>
    /// <param name="exact">true: keyword match only (titles, descriptions); false: also by meaning (slower, loads the model on first use).</param>
    /// <exception cref="Exceptions.MlServiceException">ML service unavailable or returned an error.</exception>
    Task<IReadOnlyList<int>> SearchAsync(
        string query,
        bool exact,
        int limit,
        CancellationToken cancellationToken = default);
}

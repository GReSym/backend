using GReSym.Core.Models;

namespace GReSym.Core.Interfaces;

/// <summary>State of the game vectors kept by the ML service.</summary>
public interface IGameVectorIndex
{
    /// <summary>Games whose vector is out of date, best candidates first. Scans every game (seconds).</summary>
    /// <param name="reasons">Only games with any of these <see cref="StaleReasons"/>; empty = all.</param>
    /// <exception cref="Exceptions.MlServiceException">ML service unavailable or returned an error.</exception>
    Task<StaleGamesReport> GetStaleGamesAsync(
        IReadOnlyCollection<string> reasons,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);
}

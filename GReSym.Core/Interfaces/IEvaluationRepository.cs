using GReSym.Core.Entities.Evaluation;
using GReSym.Core.Models;

namespace GReSym.Core.Interfaces;

/// <summary>Collected Steam users and evaluation runs. Changes are saved by <see cref="SaveChangesAsync"/>.</summary>
public interface IEvaluationRepository
{
    /// <summary>Steam app id → game id, only for the given app ids that are in the catalogue.</summary>
    Task<Dictionary<int, int>> GetGameIdsBySteamAppIdsAsync(IReadOnlyCollection<int> steamAppIds);

    /// <summary>Name and hash of every collected user (a few hundred rows).</summary>
    Task<List<(string Name, string SteamIdHash)>> GetSteamUserKeysAsync();

    Task AddSteamUsersAsync(IEnumerable<SteamUser> users);

    Task<SteamUsersSummary> GetSteamUsersSummaryAsync();

    Task AddRunAsync(EvaluationRun run);

    /// <summary>Tracked, so changes are saved by <see cref="SaveChangesAsync"/>.</summary>
    Task<EvaluationRun?> GetRunAsync(int id);

    /// <summary>Newest first, without the results JSON.</summary>
    Task<List<EvaluationRun>> GetRunsAsync(int page, int pageSize);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using GReSym.Core.Entities.Evaluation;
using GReSym.Core.Interfaces;
using GReSym.Core.Models;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class EvaluationRepository : IEvaluationRepository
{
    // Keeps the IN (...) lists of the app id lookup reasonably short
    private const int LookupBatchSize = 5000;

    private readonly ApplicationDbContext _context;

    public EvaluationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, int>> GetGameIdsBySteamAppIdsAsync(IReadOnlyCollection<int> steamAppIds)
    {
        var result = new Dictionary<int, int>();

        foreach (var batch in steamAppIds.Distinct().Chunk(LookupBatchSize))
        {
            var rows = await _context.SteamSources
                .AsNoTracking()
                .Where(s => s.GameId != null && batch.Contains(s.SteamAppId))
                .Select(s => new { s.SteamAppId, GameId = s.GameId!.Value })
                .ToListAsync();

            foreach (var row in rows)
                result[row.SteamAppId] = row.GameId;
        }

        return result;
    }

    public async Task<List<(string Name, string SteamIdHash)>> GetSteamUserKeysAsync()
    {
        var rows = await _context.SteamUsers
            .AsNoTracking()
            .Select(u => new { u.Name, u.SteamIdHash })
            .ToListAsync();

        return rows.Select(r => (r.Name, r.SteamIdHash)).ToList();
    }

    public async Task AddSteamUsersAsync(IEnumerable<SteamUser> users)
    {
        await _context.SteamUsers.AddRangeAsync(users);
    }

    public async Task<SteamUsersSummary> GetSteamUsersSummaryAsync()
    {
        var users = await _context.SteamUsers.CountAsync();
        var games = await _context.SteamUserGames.CountAsync();
        var reviewed = await _context.SteamUserGames.CountAsync(g => g.Recommended != null);

        return new SteamUsersSummary(users, games, reviewed, users == 0 ? 0 : Math.Round((double)games / users, 1));
    }

    public async Task AddRunAsync(EvaluationRun run)
    {
        await _context.EvaluationRuns.AddAsync(run);
    }

    public async Task<EvaluationRun?> GetRunAsync(int id)
    {
        return await _context.EvaluationRuns.FindAsync(id);
    }

    public async Task<List<EvaluationRun>> GetRunsAsync(int page, int pageSize)
    {
        return await _context.EvaluationRuns
            .AsNoTracking()
            .OrderByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new EvaluationRun
            {
                Id = r.Id,
                Status = r.Status,
                Parameters = r.Parameters,
                Error = r.Error,
                UsersEvaluated = r.UsersEvaluated,
                CreatedAt = r.CreatedAt,
                StartedAt = r.StartedAt,
                FinishedAt = r.FinishedAt,
            })
            .ToListAsync();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
